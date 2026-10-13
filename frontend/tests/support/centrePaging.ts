import type { CentrePaging, CentreRow, CentreSortField } from '@/features/vas/api/vasRepository';
import type { PagedResponse } from '@/shared/models/reporting';

const CENTRE_SORT_VALUE: Record<CentreSortField, (row: CentreRow) => number | string | null> = {
  centreName: (row) => row.centreName,
  received: (row) => row.received,
  pending: (row) => row.pending,
  averageTurnaroundDays: (row) => row.averageTurnaroundDays,
  capacityUtilisation: (row) => row.capacityUtilisationPercent,
};

/** Orders and pages the fixture rows the way the API does, so a fake backend behaves like the real one. */
export function pageCentres(rows: CentreRow[], paging: CentrePaging): PagedResponse<CentreRow> {
  const valueOf = CENTRE_SORT_VALUE[paging.sortBy];
  const sign = paging.sortDirection === 'Descending' ? -1 : 1;

  const sorted = [...rows].sort((a, b) => {
    const [left, right] = [valueOf(a), valueOf(b)];
    if (left === null || right === null) {
      return left === right ? 0 : left === null ? 1 : -1;
    }

    const order = typeof left === 'string' ? left.localeCompare(String(right)) : left - Number(right);
    return order !== 0 ? order * sign : a.centreCode.localeCompare(b.centreCode);
  });

  const totalPages = Math.ceil(sorted.length / paging.pageSize);
  const start = (paging.page - 1) * paging.pageSize;

  return {
    items: sorted.slice(start, start + paging.pageSize),
    totalCount: sorted.length,
    page: paging.page,
    pageSize: paging.pageSize,
    totalPages,
    hasPreviousPage: paging.page > 1,
    hasNextPage: paging.page < totalPages,
  };
}
