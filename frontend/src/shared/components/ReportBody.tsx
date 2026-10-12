import type { ReactNode } from 'react';
import { t } from '@/core/i18n';
import type { ReportState } from '@/shared/hooks/useReportQuery';
import type { BreakdownRow } from '@/shared/models/reporting';
import { toVolumeBars } from '@/shared/utils/chartMappers';
import { formatDays, formatNumber, formatPercent } from '@/shared/utils/format';
import { BarBreakdown, type BarItem } from './BarBreakdown';
import { DataTable, type Column } from './DataTable';
import { ChartPanel } from './Panel';
import { EmptyState, ErrorState, ReportSkeleton } from './states';

interface ReportBodyProps<T> {
  state: ReportState<T>;
  skeletonCards?: number;
  children(data: T): ReactNode;
}

/** Renders exactly one of the four states of a report: loading, error, empty or content. */
export function ReportBody<T>({ state, skeletonCards, children }: ReportBodyProps<T>) {
  switch (state.status) {
    case 'loading':
      return <ReportSkeleton cards={skeletonCards} />;
    case 'error':
      return <ErrorState error={state.error} onRetry={state.retry} />;
    case 'empty':
      return <EmptyState />;
    case 'content':
      return (
        <div className="report-stack" aria-busy={state.isRefreshing} data-refreshing={state.isRefreshing}>
          {children(state.data)}
        </div>
      );
  }
}

export function breakdownColumns(nameHeader: string): Column<BreakdownRow>[] {
  return [
    { key: 'name', header: nameHeader, render: (row) => row.name },
    { key: 'received', header: t('column.received'), numeric: true, render: (row) => formatNumber(row.received) },
    { key: 'processed', header: t('column.processed'), numeric: true, render: (row) => formatNumber(row.processed) },
    { key: 'pending', header: t('column.pending'), numeric: true, render: (row) => formatNumber(row.pending) },
    { key: 'approvalRate', header: t('column.approvalRate'), numeric: true, render: (row) => formatPercent(row.approvalRate) },
    { key: 'turnaround', header: t('column.turnaround'), numeric: true, render: (row) => formatDays(row.averageTurnaroundDays) },
    { key: 'share', header: t('column.share'), numeric: true, render: (row) => formatPercent(row.sharePercent) },
  ];
}

interface BreakdownPanelProps {
  title: string;
  subtitle?: string;
  nameHeader: string;
  rows: BreakdownRow[];
  /** How the rows become bars. Left out, bars show applications received. */
  toBars?(rows: BreakdownRow[]): BarItem[];
  max?: number;
}

/** A breakdown as bars, with the full figures one click away as a table. */
export function BreakdownPanel({ title, subtitle, nameHeader, rows, toBars = toVolumeBars, max }: BreakdownPanelProps) {
  return (
    <ChartPanel
      title={title}
      subtitle={subtitle}
      chart={<BarBreakdown items={toBars(rows)} max={max} ariaLabel={title} />}
      table={<DataTable caption={title} columns={breakdownColumns(nameHeader)} rows={rows} rowKey={(row) => row.key} />}
    />
  );
}
