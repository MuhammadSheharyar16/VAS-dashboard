import { t } from '@/core/i18n';
import { DataTable, type Column } from '@/shared/components/DataTable';
import { Panel, Stagger } from '@/shared/components/Panel';
import { StatusBadge } from '@/shared/components/StatusBadge';
import { formatDays, formatNumber, formatPercent } from '@/shared/utils/format';
import type { CentreRow, VasCentres } from '../api/vasRepository';
import type { CentreTableState } from '../useVasViewModel';

function CapacityCell({ row }: { row: CentreRow }) {
  const width = Math.min(row.capacityUtilisationPercent ?? 0, 100);

  return (
    <span className="meter">
      <span className="meter__track" aria-hidden="true">
        <span className="meter__fill" style={{ inlineSize: `${width}%` }} />
      </span>
      {formatPercent(row.capacityUtilisationPercent)}
      <StatusBadge status={row.capacityStatus} />
    </span>
  );
}

const COLUMNS: Column<CentreRow>[] = [
  { key: 'centre', header: t('filters.centre'), sortField: 'centreName', render: (row) => row.centreName },
  { key: 'country', header: t('column.country'), render: (row) => row.countryName },
  { key: 'received', header: t('column.received'), numeric: true, sortField: 'received', render: (row) => formatNumber(row.received) },
  { key: 'pending', header: t('column.pending'), numeric: true, sortField: 'pending', render: (row) => formatNumber(row.pending) },
  { key: 'approvalRate', header: t('column.approvalRate'), numeric: true, render: (row) => formatPercent(row.approvalRate) },
  {
    key: 'turnaround',
    header: t('column.turnaround'),
    numeric: true,
    sortField: 'averageTurnaroundDays',
    render: (row) => formatDays(row.averageTurnaroundDays),
  },
  { key: 'capacity', header: t('column.dailyCapacity'), numeric: true, render: (row) => formatNumber(row.dailyCapacity) },
  {
    key: 'capacityUse',
    header: t('column.capacityUse'),
    sortField: 'capacityUtilisation',
    render: (row) => <CapacityCell row={row} />,
  },
];

interface CentresSectionProps {
  report: VasCentres;
  table: CentreTableState;
}

/** Centre performance. The server sorts and pages the table; the app only asks for a page. */
export function CentresSection({ report, table }: CentresSectionProps) {
  const { centres } = report;

  return (
    <Stagger className="report-stack">
      <Panel title={t('vas.centres.tableTitle')} subtitle={t('vas.centres.capacitySubtitle')}>
        <DataTable
          caption={t('vas.centres.tableTitle')}
          columns={COLUMNS}
          rows={centres.items}
          rowKey={(row) => row.centreCode}
          sort={{ field: table.sortBy, direction: table.sortDirection, onSort: table.setSort }}
          paging={{
            page: centres.page,
            totalPages: centres.totalPages,
            totalCount: centres.totalCount,
            onPage: table.setPage,
          }}
        />
      </Panel>
    </Stagger>
  );
}
