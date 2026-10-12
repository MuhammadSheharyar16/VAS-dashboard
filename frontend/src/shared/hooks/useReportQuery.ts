import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { ReportFilterState } from './useReportFilters';
import { toReportQuery, type ReportQuery } from '@/shared/models/reporting';

/** What a screen needs to know about its report: which of the four states it is in. */
export type ReportState<T> =
  | { status: 'loading' }
  | { status: 'error'; error: unknown; retry(): void }
  | { status: 'empty'; data: T }
  | { status: 'content'; data: T; isRefreshing: boolean };

interface UseReportQueryOptions<T> {
  /** Identifies the report in the cache, together with its filters. */
  key: readonly unknown[];
  filterState: ReportFilterState;
  fetch(query: ReportQuery, signal: AbortSignal): Promise<T>;
  /** True when the report came back with nothing to show. */
  isEmpty(report: T): boolean;
  /** Anything else the request depends on, such as a table's page and sort. */
  params?: unknown;
}

/**
 * Loads one report for the current filters. While an unusable custom range is on screen the
 * report is not requested; the last good report stays up while a new one loads.
 */
export function useReportQuery<T>({ key, filterState, fetch, isEmpty, params }: UseReportQueryOptions<T>): ReportState<T> {
  const query = toReportQuery(filterState.filters);

  const result = useQuery({
    queryKey: [...key, query, params ?? null],
    queryFn: ({ signal }) => fetch(query, signal),
    enabled: filterState.problem === null,
    // Keep the last result on screen only while the same report reloads, never across reports.
    placeholderData: (previous, previousQuery) =>
      previousQuery && key.every((part, index) => previousQuery.queryKey[index] === part)
        ? keepPreviousData(previous)
        : undefined,
  });

  if (result.isError) {
    return { status: 'error', error: result.error, retry: () => void result.refetch() };
  }

  if (result.data === undefined) {
    return { status: 'loading' };
  }

  if (isEmpty(result.data)) {
    return { status: 'empty', data: result.data };
  }

  return { status: 'content', data: result.data, isRefreshing: result.isPlaceholderData || result.isFetching };
}
