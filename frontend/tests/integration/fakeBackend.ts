import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import type { Page, Route } from '@playwright/test';
import { CENTRE_SORT_FIELDS, type CentreRow, type VasCentres } from '@/features/vas/api/vasRepository';
import type { SortDirection } from '@/shared/models/reporting';
import { pageCentres } from '../support/centrePaging';

// The app under test is the real one: real sign-in code, real API client. Only the network is
// faked here, at the two places the browser would leave the machine: Microsoft Entra ID and the API.

const TENANT_ID = '11111111-1111-1111-1111-111111111111';
const API_CLIENT_ID = '22222222-2222-2222-2222-222222222222';
const SPA_CLIENT_ID = '33333333-3333-3333-3333-333333333333';
const USER_OBJECT_ID = '44444444-4444-4444-4444-444444444444';

const ENTRA_HOST = 'https://login.microsoftonline.com';
const AUTHORITY = `${ENTRA_HOST}/${TENANT_ID}`;
const API_SCOPE = `api://${API_CLIENT_ID}/Dashboard.Read`;

export const TEST_ACCESS_TOKEN = 'test-access-token';

const FIXTURE_BY_PATH: Record<string, string> = {
  '/auth/me': 'auth-me',
  '/auth/me/permissions': 'auth-permissions',
  '/auth/me/menu': 'auth-menu',
  '/executive-summary': 'executive-summary',
  '/vas/applications': 'vas-applications',
  '/vas/outcomes': 'vas-outcomes',
  '/vas/processing': 'vas-processing',
  '/vas/revenue': 'vas-revenue',
  '/vas/visa-types': 'vas-visa-types',
  '/vas/centres': 'vas-centres',
  '/lookups/countries': 'lookups-countries',
  '/lookups/centres': 'lookups-centres',
  '/lookups/visa-types': 'lookups-visa-types',
};

function loadFixture(name: string): unknown {
  const path = fileURLToPath(new URL(`../fixtures/${name}.json`, import.meta.url));
  return JSON.parse(readFileSync(path, 'utf8'));
}

const base64Url = (value: unknown) => Buffer.from(JSON.stringify(value)).toString('base64url');

const CORS = { 'access-control-allow-origin': '*', 'access-control-allow-headers': '*' };

const json = (route: Route, body: unknown, status = 200) =>
  route.fulfill({ status, contentType: 'application/json', headers: CORS, body: JSON.stringify(body) });

/** An unsigned token in the shape Entra ID issues. The browser never checks the signature; the API would. */
function idToken(nonce: string): string {
  const now = Math.floor(Date.now() / 1000);
  const claims = {
    aud: SPA_CLIENT_ID,
    iss: `${AUTHORITY}/v2.0`,
    iat: now,
    nbf: now,
    exp: now + 3600,
    name: 'Test Executive',
    preferred_username: 'executive@example.test',
    oid: USER_OBJECT_ID,
    sub: USER_OBJECT_ID,
    tid: TENANT_ID,
    ver: '2.0',
    nonce,
  };

  return `${base64Url({ alg: 'RS256', typ: 'JWT', kid: 'test-key' })}.${base64Url(claims)}.c2lnbmF0dXJl`;
}

async function fakeEntra(route: Route, state: { nonce: string }): Promise<void> {
  const request = route.request();
  const url = new URL(request.url());

  if (request.method() === 'OPTIONS') {
    await route.fulfill({ status: 204, headers: CORS });
  } else if (url.pathname.endsWith('/.well-known/openid-configuration')) {
    await json(route, {
      issuer: `${AUTHORITY}/v2.0`,
      authorization_endpoint: `${AUTHORITY}/oauth2/v2.0/authorize`,
      token_endpoint: `${AUTHORITY}/oauth2/v2.0/token`,
      end_session_endpoint: `${AUTHORITY}/oauth2/v2.0/logout`,
      jwks_uri: `${AUTHORITY}/discovery/v2.0/keys`,
    });
  } else if (url.pathname.endsWith('/discovery/instance')) {
    await json(route, {
      tenant_discovery_endpoint: `${AUTHORITY}/v2.0/.well-known/openid-configuration`,
      metadata: [
        {
          preferred_network: 'login.microsoftonline.com',
          preferred_cache: 'login.windows.net',
          aliases: ['login.microsoftonline.com', 'login.windows.net', 'login.microsoft.com', 'sts.windows.net'],
        },
      ],
    });
  } else if (url.pathname.endsWith('/oauth2/v2.0/authorize')) {
    // The sign-in page: "signs in" at once and sends the browser back with an authorization code.
    state.nonce = url.searchParams.get('nonce') ?? '';
    const back = new URL(url.searchParams.get('redirect_uri') ?? '');
    back.hash = new URLSearchParams({
      code: 'test-authorization-code',
      client_info: base64Url({ uid: USER_OBJECT_ID, utid: TENANT_ID }),
      state: url.searchParams.get('state') ?? '',
    }).toString();

    await route.fulfill({
      status: 200,
      contentType: 'text/html',
      body: `<!doctype html><script>location.replace(${JSON.stringify(back.toString())})</script>`,
    });
  } else if (url.pathname.endsWith('/oauth2/v2.0/token')) {
    await json(route, {
      token_type: 'Bearer',
      scope: `${API_SCOPE} openid profile offline_access`,
      expires_in: 3600,
      ext_expires_in: 3600,
      access_token: TEST_ACCESS_TOKEN,
      refresh_token: 'test-refresh-token',
      id_token: idToken(state.nonce),
      client_info: base64Url({ uid: USER_OBJECT_ID, utid: TENANT_ID }),
    });
  } else {
    await route.fulfill({ status: 404, headers: CORS });
  }
}

export interface ApiCall {
  path: string;
  query: URLSearchParams;
  authorization: string | undefined;
}

async function fakeApi(route: Route, calls: ApiCall[]): Promise<void> {
  const request = route.request();
  const url = new URL(request.url());
  const path = url.pathname.replace(/^\/api\/v1/, '');
  const authorization = request.headers().authorization;

  calls.push({ path, query: url.searchParams, authorization });

  if (authorization !== `Bearer ${TEST_ACCESS_TOKEN}`) {
    await json(route, { success: false, message: 'Authentication is required.', data: null, errors: [], traceId: 'test' }, 401);
    return;
  }

  const fixture = FIXTURE_BY_PATH[path];
  if (!fixture) {
    await json(route, { success: false, message: 'Resource not found.', data: null, errors: [], traceId: 'test' }, 404);
    return;
  }

  let data = loadFixture(fixture);
  if (path === '/vas/centres') {
    const report = data as VasCentres;
    const rows: CentreRow[] = report.centres.items;
    data = {
      ...report,
      centres: pageCentres(rows, {
        page: Number(url.searchParams.get('page') ?? 1),
        pageSize: Number(url.searchParams.get('pageSize') ?? 10),
        sortBy: CENTRE_SORT_FIELDS.find((field) => field === url.searchParams.get('sortBy')) ?? 'centreName',
        sortDirection: (url.searchParams.get('sortDirection') ?? 'Ascending') as SortDirection,
      }),
    };
  }

  await json(route, { success: true, message: 'Operation completed successfully.', data, errors: [], traceId: null });
}

/**
 * Stands in for Entra ID and for the API. Returns the API calls the app made, so a test can check
 * what was asked for and that the sign-in token travelled with it.
 */
export async function installFakeBackend(page: Page): Promise<ApiCall[]> {
  const calls: ApiCall[] = [];
  const state = { nonce: '' };

  await page.route('**/config.json', (route) =>
    json(route, { clientId: SPA_CLIENT_ID, authority: AUTHORITY, apiScope: API_SCOPE }),
  );
  await page.route(`${ENTRA_HOST}/**`, (route) => fakeEntra(route, state));
  await page.route('**/api/v1/**', (route) => fakeApi(route, calls));

  return calls;
}
