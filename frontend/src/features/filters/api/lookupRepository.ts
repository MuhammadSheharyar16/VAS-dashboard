import type { ApiClient } from '@/core/http/apiClient';
import { loadFixture, MOCK_DELAY_MS } from '@/mocks';
import type { LookupItem } from '@/shared/models/reporting';

export interface LookupRepository {
  getCountries(): Promise<LookupItem[]>;
  getCentres(): Promise<LookupItem[]>;
  getVisaTypes(): Promise<LookupItem[]>;
}

export function createLiveLookupRepository(api: ApiClient): LookupRepository {
  return {
    getCountries: () => api.get<LookupItem[]>('lookupCountries'),
    getCentres: () => api.get<LookupItem[]>('lookupCentres'),
    getVisaTypes: () => api.get<LookupItem[]>('lookupVisaTypes'),
  };
}

export function createMockLookupRepository(delayMs = MOCK_DELAY_MS): LookupRepository {
  return {
    getCountries: () => loadFixture<LookupItem[]>('lookups-countries', delayMs),
    getCentres: () => loadFixture<LookupItem[]>('lookups-centres', delayMs),
    getVisaTypes: () => loadFixture<LookupItem[]>('lookups-visa-types', delayMs),
  };
}
