import type { ApiClient } from '@/core/http/apiClient';
import { echoFilters, loadFixture, MOCK_DELAY_MS } from '@/mocks';
import type { ApplicationsTrend, KpiCard, OutcomeShare, ReportMeta, ReportQuery } from '@/shared/models/reporting';

export interface CountryVolume {
  countryCode: string;
  countryName: string;
  received: number;
  sharePercent: number | null;
}

export interface ExecutiveSummary {
  meta: ReportMeta;
  kpis: KpiCard[];
  applicationsTrend: ApplicationsTrend;
  outcomes: OutcomeShare[];
  topCountries: CountryVolume[];
}

export interface ExecutiveSummaryRepository {
  get(query: ReportQuery, signal?: AbortSignal): Promise<ExecutiveSummary>;
}

export function createLiveExecutiveSummaryRepository(api: ApiClient): ExecutiveSummaryRepository {
  return {
    get: (query, signal) => api.get<ExecutiveSummary>('executiveSummary', query, signal),
  };
}

export function createMockExecutiveSummaryRepository(delayMs = MOCK_DELAY_MS): ExecutiveSummaryRepository {
  return {
    get: async (query) => echoFilters(await loadFixture<ExecutiveSummary>('executive-summary', delayMs), query),
  };
}
