import type { ApiClient } from '@/core/http/apiClient';
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
