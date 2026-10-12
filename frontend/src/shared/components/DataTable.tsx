import { ArrowDown, ArrowUp, ArrowUpDown, ChevronLeft, ChevronRight } from 'lucide-react';
import type { ReactNode } from 'react';
import { t } from '@/core/i18n';
import type { SortDirection } from '@/shared/models/reporting';
import { EmptyState } from './states';

export interface Column<T> {
  key: string;
  header: string;
  render(row: T): ReactNode;
  /** Numbers are right-aligned with tabular figures so they line up. */
  numeric?: boolean;
  /** The sort field the server accepts for this column. Leave out for a column that cannot be sorted. */
  sortField?: string;
}

export interface TableSort {
  field: string | null;
  direction: SortDirection;
  onSort(field: string, direction: SortDirection): void;
}

export interface TablePaging {
  page: number;
  totalPages: number;
  totalCount: number;
  onPage(page: number): void;
}

interface DataTableProps<T> {
  caption: string;
  columns: Column<T>[];
  rows: T[];
  rowKey(row: T): string;
  sort?: TableSort;
  paging?: TablePaging;
}

/** A plain, accessible table. Sorting and paging are requests to the server, never done on a full download. */
export function DataTable<T>({ caption, columns, rows, rowKey, sort, paging }: DataTableProps<T>) {
  if (rows.length === 0) {
    return <EmptyState variant="noMatches" />;
  }

  return (
    <>
      <div className="table-wrap">
        <table className="table">
          <caption className="visually-hidden">{caption}</caption>
          <thead>
            <tr>
              {columns.map((column) => {
                const direction =
                  sort && column.sortField !== undefined && sort.field === column.sortField ? sort.direction : null;
                const ariaSort = direction === null ? undefined : direction === 'Ascending' ? 'ascending' : 'descending';
                const next: SortDirection = direction === 'Descending' ? 'Ascending' : 'Descending';
                const SortIcon = direction === null ? ArrowUpDown : direction === 'Ascending' ? ArrowUp : ArrowDown;

                return (
                  <th key={column.key} scope="col" className={column.numeric ? 'is-number' : undefined} aria-sort={ariaSort}>
                    {sort && column.sortField ? (
                      <button
                        type="button"
                        className="table__sort"
                        onClick={() => sort.onSort(column.sortField!, next)}
                        aria-label={t(next === 'Ascending' ? 'table.sortAscending' : 'table.sortDescending', {
                          column: column.header,
                        })}
                      >
                        {column.header}
                        <SortIcon size={13} aria-hidden="true" />
                      </button>
                    ) : (
                      column.header
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={rowKey(row)}>
                {columns.map((column) => (
                  <td key={column.key} className={column.numeric ? 'is-number' : undefined}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {paging && (
        <div className="table__footer">
          <span>{t('table.rows', { count: paging.totalCount })}</span>
          <span className="table__pager">
            <button
              type="button"
              className="button button--icon"
              disabled={paging.page <= 1}
              onClick={() => paging.onPage(paging.page - 1)}
              aria-label={t('table.previous')}
            >
              <ChevronLeft size={16} aria-hidden="true" />
            </button>
            <span aria-live="polite">{t('table.page', { page: paging.page, pages: Math.max(paging.totalPages, 1) })}</span>
            <button
              type="button"
              className="button button--icon"
              disabled={paging.page >= paging.totalPages}
              onClick={() => paging.onPage(paging.page + 1)}
              aria-label={t('table.next')}
            >
              <ChevronRight size={16} aria-hidden="true" />
            </button>
          </span>
        </div>
      )}
    </>
  );
}
