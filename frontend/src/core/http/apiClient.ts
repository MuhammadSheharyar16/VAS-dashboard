import { ENDPOINTS, type EndpointKey } from './endpoints';

export interface ApiFieldError {
  field: string | null;
  message: string;
}

/** The envelope every API response uses. */
interface ApiEnvelope<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: ApiFieldError[];
  traceId: string | null;
}

export type ApiErrorKind = 'unauthenticated' | 'forbidden' | 'notFound' | 'validation' | 'rateLimited' | 'server' | 'network';

export class ApiError extends Error {
  constructor(
    readonly kind: ApiErrorKind,
    readonly status: number,
    readonly fieldErrors: ApiFieldError[] = [],
    readonly traceId: string | null = null,
  ) {
    super(kind);
  }
}

export type QueryValue = string | number | boolean | null | undefined;

export interface TokenSource {
  /** Returns a token for the API. `forceRefresh` asks the identity provider for a new one. */
  getAccessToken(forceRefresh?: boolean): Promise<string>;
}

function kindFor(status: number): ApiErrorKind {
  switch (status) {
    case 400:
    case 422:
      return 'validation';
    case 401:
      return 'unauthenticated';
    case 403:
      return 'forbidden';
    case 404:
      return 'notFound';
    case 429:
      return 'rateLimited';
    default:
      return 'server';
  }
}

export function buildUrl(baseUrl: string, endpoint: EndpointKey, query: Record<string, QueryValue> = {}): string {
  const search = new URLSearchParams();

  for (const [name, value] of Object.entries(query)) {
    if (value !== null && value !== undefined && value !== '') {
      search.set(name, String(value));
    }
  }

  const queryString = search.toString();
  return `${baseUrl}${ENDPOINTS[endpoint]}${queryString ? `?${queryString}` : ''}`;
}

export interface ApiClient {
  get<T>(endpoint: EndpointKey, query?: Record<string, QueryValue>, signal?: AbortSignal): Promise<T>;
}

/**
 * The one HTTP client. It adds the bearer token, unwraps the envelope and turns failures into ApiError.
 * On a 401 it asks for a fresh token once and retries once; concurrent 401s share that one refresh.
 */
export function createApiClient(baseUrl: string, tokens: TokenSource, fetcher: typeof fetch = fetch): ApiClient {
  let refreshInFlight: Promise<string> | null = null;

  const refreshToken = (): Promise<string> => {
    refreshInFlight ??= tokens.getAccessToken(true).finally(() => {
      refreshInFlight = null;
    });
    return refreshInFlight;
  };

  const send = async (url: string, token: string, signal?: AbortSignal): Promise<Response> => {
    try {
      return await fetcher(url, {
        method: 'GET',
        headers: { Accept: 'application/json', Authorization: `Bearer ${token}` },
        signal,
      });
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        throw error;
      }

      throw new ApiError('network', 0);
    }
  };

  return {
    async get<T>(endpoint: EndpointKey, query: Record<string, QueryValue> = {}, signal?: AbortSignal): Promise<T> {
      const url = buildUrl(baseUrl, endpoint, query);

      let response = await send(url, await tokens.getAccessToken(), signal);
      if (response.status === 401) {
        response = await send(url, await refreshToken(), signal);
      }

      let envelope: ApiEnvelope<T> | null;
      try {
        envelope = (await response.json()) as ApiEnvelope<T>;
      } catch {
        envelope = null;
      }

      if (!response.ok || !envelope?.success || envelope.data === null) {
        throw new ApiError(kindFor(response.status), response.status, envelope?.errors ?? [], envelope?.traceId ?? null);
      }

      return envelope.data;
    },
  };
}
