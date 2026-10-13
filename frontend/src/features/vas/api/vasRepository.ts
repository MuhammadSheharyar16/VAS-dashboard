import type { ApiClient } from '@/core/http/apiClient';
import type {
  ApplicationsTrend,
  BreakdownRow,
  CountShare,
  KpiCard,
  KpiStatus,
  OutcomeShare,
  PagedResponse,
  ReportMeta,
  ReportQuery,
  SortDirection,
  TrendGranularity,
} from '@/shared/models/reporting';

export interface VasApplications {
  meta: ReportMeta;
  kpis: KpiCard[];
  trend: ApplicationsTrend;
  byCountry: BreakdownRow[];
  byVisaType: BreakdownRow[];
  byChannel: BreakdownRow[];
}

export interface OutcomeTrendPoint {
  periodStart: string;
  approved: number;
  rejected: number;
  withdrawn: number;
}

export interface VasOutcomes {
  meta: ReportMeta;
  kpis: KpiCard[];
  trend: { granularity: TrendGranularity; points: OutcomeTrendPoint[] };
  outcomes: OutcomeShare[];
  byVisaType: BreakdownRow[];
  byCountry: BreakdownRow[];
}

export interface StageFunnelRow {
  stage: string;
  reached: number;
  completed: number;
  completionPercent: number | null;
}

export interface VasProcessing {
  meta: ReportMeta;
  kpis: KpiCard[];
  statuses: CountShare[];
  pendingByStage: CountShare[];
  stageFunnel: StageFunnelRow[];
  channels: BreakdownRow[];
  turnaroundByVisaType: BreakdownRow[];
}

export interface RevenueTrendPoint {
  periodStart: string;
  currency: string;
  amount: number;
  transactions: number;
}

export interface RevenueBreakdownRow {
  key: string;
  name: string;
  currency: string;
  amount: number;
  transactions: number;
  sharePercent: number | null;
}

export interface VasRevenue {
  meta: ReportMeta;
  kpis: KpiCard[];
  trend: { granularity: TrendGranularity; points: RevenueTrendPoint[] };
  byCountry: RevenueBreakdownRow[];
  byCentre: RevenueBreakdownRow[];
  byVisaType: RevenueBreakdownRow[];
  byService: RevenueBreakdownRow[];
}

export interface VasVisaTypes {
  meta: ReportMeta;
  visaTypes: BreakdownRow[];
}

export interface CentreRow {
  centreCode: string;
  centreName: string;
  countryCode: string;
  countryName: string;
  city: string;
  dailyCapacity: number;
  received: number;
  processed: number;
  pending: number;
  approvalRate: number | null;
  averageTurnaroundDays: number | null;
  averageDailyApplications: number;
  capacityUtilisationPercent: number | null;
  capacityStatus: KpiStatus;
}

export interface VasCentres {
  meta: ReportMeta;
  centres: PagedResponse<CentreRow>;
}

/** The sort fields the centres endpoint accepts. Nothing else is ever sent as a sort. */
export const CENTRE_SORT_FIELDS = ['centreName', 'received', 'pending', 'averageTurnaroundDays', 'capacityUtilisation'] as const;
export type CentreSortField = (typeof CENTRE_SORT_FIELDS)[number];

export interface CentrePaging {
  page: number;
  pageSize: number;
  sortBy: CentreSortField;
  sortDirection: SortDirection;
}

export interface VasRepository {
  getApplications(query: ReportQuery, signal?: AbortSignal): Promise<VasApplications>;
  getOutcomes(query: ReportQuery, signal?: AbortSignal): Promise<VasOutcomes>;
  getProcessing(query: ReportQuery, signal?: AbortSignal): Promise<VasProcessing>;
  getRevenue(query: ReportQuery, signal?: AbortSignal): Promise<VasRevenue>;
  getVisaTypes(query: ReportQuery, signal?: AbortSignal): Promise<VasVisaTypes>;
  getCentres(query: ReportQuery, paging: CentrePaging, signal?: AbortSignal): Promise<VasCentres>;
}

export function createLiveVasRepository(api: ApiClient): VasRepository {
  return {
    getApplications: (query, signal) => api.get<VasApplications>('vasApplications', query, signal),
    getOutcomes: (query, signal) => api.get<VasOutcomes>('vasOutcomes', query, signal),
    getProcessing: (query, signal) => api.get<VasProcessing>('vasProcessing', query, signal),
    getRevenue: (query, signal) => api.get<VasRevenue>('vasRevenue', query, signal),
    getVisaTypes: (query, signal) => api.get<VasVisaTypes>('vasVisaTypes', query, signal),
    getCentres: (query, paging, signal) => api.get<VasCentres>('vasCentres', { ...query, ...paging }, signal),
  };
}
