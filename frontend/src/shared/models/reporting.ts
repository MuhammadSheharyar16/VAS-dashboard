// The shapes the API returns, shared by every report. The API sends these ready to show:
// the app formats them and never recalculates a KPI.

export type KpiUnit = 'Count' | 'Percent' | 'Days' | 'Currency';
export type KpiStatus = 'Neutral' | 'Good' | 'Warning' | 'Critical';
export type TrendDirection = 'Flat' | 'Up' | 'Down';
export type TrendGranularity = 'Day' | 'Month';
export type ComparisonPeriod = 'None' | 'PreviousPeriod';
export type DataSource = 'Mock' | 'Fabric';
export type SortDirection = 'Ascending' | 'Descending';

export interface KpiCard {
  key: string;
  value: number | null;
  unit: KpiUnit;
  currency: string | null;
  previousValue: number | null;
  changePercent: number | null;
  trend: TrendDirection;
  status: KpiStatus;
}

export interface AppliedFilter {
  from: string;
  toExclusive: string;
  countryCode: string | null;
  centreCode: string | null;
  visaTypeCode: string | null;
  compare: ComparisonPeriod;
  previousFrom: string | null;
  previousToExclusive: string | null;
}

export interface ReportMeta {
  generatedAtUtc: string;
  dataSource: DataSource;
  freshness: { lastRefreshedAtUtc: string | null };
  filter: AppliedFilter;
  thresholdsArePlaceholders: boolean;
}

export interface TrendPoint {
  periodStart: string;
  received: number;
  decided: number;
}

export interface ApplicationsTrend {
  granularity: TrendGranularity;
  points: TrendPoint[];
}

export interface OutcomeShare {
  outcome: string;
  count: number;
  sharePercent: number | null;
}

export interface CountShare {
  key: string;
  count: number;
  sharePercent: number | null;
}

export interface BreakdownRow {
  key: string;
  name: string;
  group: string | null;
  received: number;
  processed: number;
  pending: number;
  approved: number;
  rejected: number;
  withdrawn: number;
  approvalRate: number | null;
  rejectionRate: number | null;
  averageTurnaroundDays: number | null;
  sharePercent: number | null;
}

export interface LookupItem {
  code: string;
  name: string;
  group: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** The filters as the user set them. Null means "not set"; the API then applies its default. */
export interface ReportFilters {
  /** A preset number of days, or null when a custom range is chosen. */
  lastDays: number | null;
  from: string | null;
  toExclusive: string | null;
  countryCode: string | null;
  centreCode: string | null;
  visaTypeCode: string | null;
  compare: boolean;
}

/** The query-string parameters every report endpoint accepts. */
export type ReportQuery = Record<string, string | number | null>;

export function toReportQuery(filters: ReportFilters): ReportQuery {
  return {
    lastDays: filters.lastDays,
    from: filters.lastDays === null ? filters.from : null,
    toExclusive: filters.lastDays === null ? filters.toExclusive : null,
    countryCode: filters.countryCode,
    centreCode: filters.centreCode,
    visaTypeCode: filters.visaTypeCode,
    compare: filters.compare ? 'PreviousPeriod' : 'None',
  };
}
