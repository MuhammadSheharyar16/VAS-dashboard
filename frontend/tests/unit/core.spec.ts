import { describe, expect, it, vi } from 'vitest';
import { ConfigError, readEnv } from '@/core/config/env';
import { loadRuntimeConfig } from '@/core/config/runtimeConfig';
import { ApiError, buildUrl, createApiClient } from '@/core/http/apiClient';
import { hasTranslation, t } from '@/core/i18n';
import { addDays, sanitizeCode, sanitizeDate, validateRange } from '@/core/sanitizers/filters';

describe('sanitizeCode', () => {
  it.each(['AE', 'AE-DXB', 'visa_type_1', '  TOUR  '])('accepts %s', (value) => {
    expect(sanitizeCode(value)).toBe(value.trim());
  });

  it.each([
    ['an empty value', ''],
    ['a missing value', null],
    ['spaces inside', 'AE DXB'],
    ['a quote', "AE'"],
    ['markup', '<script>'],
    ['a path', '../etc'],
    ['more than 20 characters', 'A'.repeat(21)],
  ])('rejects %s', (_label, value) => {
    expect(sanitizeCode(value)).toBeNull();
  });

  it('accepts exactly 20 characters', () => {
    expect(sanitizeCode('A'.repeat(20))).toHaveLength(20);
  });
});

describe('sanitizeDate', () => {
  it('accepts a real date written as yyyy-MM-dd', () => {
    expect(sanitizeDate('2026-02-28')).toBe('2026-02-28');
  });

  it.each(['2026-02-30', '2026-13-01', '28/02/2026', '2026-2-8', 'today', '', '2026-02-28T00:00:00Z'])(
    'rejects %s',
    (value) => {
      expect(sanitizeDate(value)).toBeNull();
    },
  );
});

describe('validateRange', () => {
  it('accepts a range of exactly 24 months', () => {
    expect(validateRange('2024-06-01', '2026-06-01')).toBeNull();
  });

  it('rejects a range one day over 24 months', () => {
    expect(validateRange('2024-06-01', '2026-06-02')).toBe('tooLong');
  });

  it('rejects an empty or reversed range', () => {
    expect(validateRange('2026-06-01', '2026-06-01')).toBe('order');
    expect(validateRange('2026-06-02', '2026-06-01')).toBe('order');
  });

  it('adds days across a month end', () => {
    expect(addDays('2026-01-31', 1)).toBe('2026-02-01');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });
});

describe('readEnv', () => {
  it('reads the base URL without a trailing slash', () => {
    expect(readEnv({ VITE_API_BASE_URL: '/api/v1/' })).toEqual({ apiBaseUrl: '/api/v1' });
  });

  it.each([
    ['the base URL is missing', {}],
    ['the base URL is empty', { VITE_API_BASE_URL: ' ' }],
    ['the base URL names a host', { VITE_API_BASE_URL: 'https://example.test/api' }],
    ['the base URL is protocol-relative', { VITE_API_BASE_URL: '//example.test/api' }],
  ])('fails closed when %s', (_label, source) => {
    expect(() => readEnv(source)).toThrow(new ConfigError('VITE_API_BASE_URL'));
  });
});

describe('loadRuntimeConfig', () => {
  const config = { clientId: 'c', authority: 'https://login.example.test/t', apiScope: 'api://a/Dashboard.Read' };

  it('returns the sign-in settings the API serves', async () => {
    const fetcher = vi.fn().mockResolvedValue(new Response(JSON.stringify(config)));

    await expect(loadRuntimeConfig(fetcher)).resolves.toEqual(config);
    expect(fetcher).toHaveBeenCalledWith('/config.json', { cache: 'no-store' });
  });

  it('fails when a setting is missing or the request fails', async () => {
    const incomplete = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ...config, clientId: '' })));
    const failed = vi.fn().mockResolvedValue(new Response('', { status: 502 }));

    await expect(loadRuntimeConfig(incomplete)).rejects.toThrow();
    await expect(loadRuntimeConfig(failed)).rejects.toThrow();
  });
});

describe('api client', () => {
  const envelope = (data: unknown, status = 200, extra: object = {}) =>
    new Response(JSON.stringify({ success: status < 400, message: '', data, errors: [], traceId: null, ...extra }), { status });

  it('builds the URL from the base, the endpoint key and the set parameters only', () => {
    expect(buildUrl('/api/v1', 'vasCentres', { countryCode: 'AE', centreCode: null, page: 2, empty: '' })).toBe(
      '/api/v1/vas/centres?countryCode=AE&page=2',
    );
  });

  it('sends the bearer token and unwraps the envelope', async () => {
    const fetcher = vi.fn().mockResolvedValue(envelope({ id: '1' }));
    const api = createApiClient('/api/v1', { getAccessToken: () => Promise.resolve('token-1') }, fetcher);

    await expect(api.get('me')).resolves.toEqual({ id: '1' });
    expect(fetcher).toHaveBeenCalledWith(
      '/api/v1/auth/me',
      expect.objectContaining({ headers: expect.objectContaining({ Authorization: 'Bearer token-1' }) }),
    );
  });

  it('on a 401 asks for a fresh token once and retries once', async () => {
    const fetcher = vi.fn().mockResolvedValueOnce(envelope(null, 401)).mockResolvedValueOnce(envelope({ ok: true }));
    const getAccessToken = vi.fn((force?: boolean) => Promise.resolve(force ? 'fresh' : 'stale'));
    const api = createApiClient('/api/v1', { getAccessToken }, fetcher);

    await expect(api.get('me')).resolves.toEqual({ ok: true });
    expect(getAccessToken).toHaveBeenCalledTimes(2);
    expect(fetcher.mock.calls[1]?.[1]).toEqual(
      expect.objectContaining({ headers: expect.objectContaining({ Authorization: 'Bearer fresh' }) }),
    );
  });

  it('shares one token refresh between requests that fail together', async () => {
    const fetcher = vi.fn((_url: string, init: RequestInit) =>
      Promise.resolve(
        (init.headers as Record<string, string>).Authorization === 'Bearer fresh' ? envelope({ ok: true }) : envelope(null, 401),
      ),
    );
    const getAccessToken = vi.fn((force?: boolean) => Promise.resolve(force ? 'fresh' : 'stale'));
    const api = createApiClient('/api/v1', { getAccessToken }, fetcher as unknown as typeof fetch);

    await Promise.all([api.get('me'), api.get('menu'), api.get('permissions')]);

    expect(getAccessToken.mock.calls.filter(([force]) => force === true)).toHaveLength(1);
  });

  it('does not retry forever when the fresh token is refused too', async () => {
    const fetcher = vi.fn().mockResolvedValue(envelope(null, 401));
    const api = createApiClient('/api/v1', { getAccessToken: () => Promise.resolve('t') }, fetcher);

    await expect(api.get('me')).rejects.toMatchObject({ kind: 'unauthenticated', status: 401 });
    expect(fetcher).toHaveBeenCalledTimes(2);
  });

  it.each([
    [403, 'forbidden'],
    [400, 'validation'],
    [404, 'notFound'],
    [429, 'rateLimited'],
    [500, 'server'],
  ])('turns a %i into a %s error carrying the field errors and the trace id', async (status, kind) => {
    const errors = [{ field: 'countryCode', message: 'Unknown country.' }];
    const fetcher = vi.fn().mockResolvedValue(envelope(null, status, { errors, traceId: 'trace-9' }));
    const api = createApiClient('/api/v1', { getAccessToken: () => Promise.resolve('t') }, fetcher);

    const error = await api.get('executiveSummary').catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ kind, status, fieldErrors: errors, traceId: 'trace-9' });
  });

  it('reports a network failure without leaking its detail', async () => {
    const fetcher = vi.fn().mockRejectedValue(new TypeError('getaddrinfo ENOTFOUND internal-host'));
    const api = createApiClient('/api/v1', { getAccessToken: () => Promise.resolve('t') }, fetcher);

    const error = await api.get('me').catch((caught: unknown) => caught);

    expect(error).toMatchObject({ kind: 'network' });
    expect((error as Error).message).not.toContain('internal-host');
  });
});

describe('i18n', () => {
  it('fills placeholders and returns the key itself when a text is missing', () => {
    expect(t('kpi.previous', { value: '1,355' })).toBe('Previous period: 1,355');
    expect(t('no.such.key')).toBe('no.such.key');
    expect(hasTranslation('kpi.approvalRate')).toBe(true);
  });
});
