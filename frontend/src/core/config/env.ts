export interface AppEnv {
  /** Relative base path of the API, for example `/api/v1`. */
  apiBaseUrl: string;
  /** True serves fixtures and a fake signed-in user. */
  useMock: boolean;
}

export class ConfigError extends Error {}

/**
 * Reads the build-time settings. Throws instead of falling back to a default host, so a
 * misconfigured build shows a configuration error rather than calling somewhere unintended.
 */
export function readEnv(source: Record<string, unknown> = import.meta.env): AppEnv {
  const apiBaseUrl = source.VITE_API_BASE_URL;
  const useMock = source.VITE_USE_MOCK;

  if (typeof apiBaseUrl !== 'string' || apiBaseUrl.trim() === '') {
    throw new ConfigError('VITE_API_BASE_URL');
  }

  if (!apiBaseUrl.startsWith('/') || apiBaseUrl.startsWith('//')) {
    throw new ConfigError('VITE_API_BASE_URL');
  }

  if (useMock !== 'true' && useMock !== 'false') {
    throw new ConfigError('VITE_USE_MOCK');
  }

  return { apiBaseUrl: apiBaseUrl.replace(/\/+$/, ''), useMock: useMock === 'true' };
}
