import { useRepositories } from '@/app/repositories';
import { useFilterLookups } from '@/features/filters/useFilterLookups';
import type { FilterLookups } from '@/shared/components/FilterBar';
import { useReportFilters, type ReportFilterState } from '@/shared/hooks/useReportFilters';
import { useReportQuery, type ReportState } from '@/shared/hooks/useReportQuery';
import type { ExecutiveSummary } from './api/executiveSummaryRepository';

export interface ExecutiveSummaryViewModel {
  filterState: ReportFilterState;
  lookups: FilterLookups;
  report: ReportState<ExecutiveSummary>;
}

/** Owns the Executive Summary screen's state: its filters, its lookups and its report. */
export function useExecutiveSummaryViewModel(): ExecutiveSummaryViewModel {
  const { executiveSummary } = useRepositories();
  const filterState = useReportFilters();
  const lookups = useFilterLookups();

  const report = useReportQuery<ExecutiveSummary>({
    key: ['executive-summary'],
    filterState,
    fetch: (query, signal) => executiveSummary.get(query, signal),
    isEmpty: (summary) => summary.kpis.find((kpi) => kpi.key === 'applicationsReceived')?.value === 0,
  });

  return { filterState, lookups, report };
}
