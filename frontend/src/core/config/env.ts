export interface AppEnv {
  /** Relative base path of the API, for example `/api/v1`. */
  apiBaseUrl: string;
}

export class ConfigError extends Error {}

/**
 * Reads the build-time settings. Throws instead of falling back to a default host, so a
 * misconfigured build shows a configuration error rather than calling somewhere unintended.
 */
export function readEnv(source: Record<string, unknown> = import.meta.env): AppEnv {
  const apiBaseUrl = source.VITE_API_BASE_URL;

  if (typeof apiBaseUrl !== 'string' || apiBaseUrl.trim() === '') {
    throw new ConfigError('VITE_API_BASE_URL');
  }

  if (!apiBaseUrl.startsWith('/') || apiBaseUrl.startsWith('//')) {
    throw new ConfigError('VITE_API_BASE_URL');
  }

  return { apiBaseUrl: apiBaseUrl.replace(/\/+$/, '') };
}
