import { ENDPOINTS } from '@/core/http/endpoints';

/** The non-secret sign-in settings the API serves, so one build works in every environment. */
export interface RuntimeConfig {
  tenantId: string;
  clientId: string;
  authority: string;
  apiScope: string;
}

function isRuntimeConfig(value: unknown): value is RuntimeConfig {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return ['tenantId', 'clientId', 'authority', 'apiScope'].every(
    (key) => typeof candidate[key] === 'string' && candidate[key] !== '',
  );
}

export async function loadRuntimeConfig(fetcher: typeof fetch = fetch): Promise<RuntimeConfig> {
  const response = await fetcher(ENDPOINTS.clientConfig, { cache: 'no-store' });
  const body: unknown = response.ok ? await response.json() : null;

  if (!isRuntimeConfig(body)) {
    throw new Error('runtime-config');
  }

  return body;
}
