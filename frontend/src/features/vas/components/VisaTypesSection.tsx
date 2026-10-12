import { t } from '@/core/i18n';
import { DataTable, type Column } from '@/shared/components/DataTable';
import { Panel, Stagger } from '@/shared/components/Panel';
import { BreakdownPanel, breakdownColumns } from '@/shared/components/ReportBody';
import type { BreakdownRow } from '@/shared/models/reporting';
import { toApprovalRateBars } from '@/shared/utils/chartMappers';
import type { VasVisaTypes } from '../api/vasRepository';

/** Application volumes and performance by visa type. */
export function VisaTypesSection({ report }: { report: VasVisaTypes }) {
  const [name, ...figures] = breakdownColumns(t('filters.visaType'));
  const columns: Column<BreakdownRow>[] = [
    ...(name ? [name] : []),
    { key: 'category', header: t('column.category'), render: (row) => row.group ?? '' },
    ...figures,
  ];

  return (
    <Stagger className="report-stack">
      <div className="grid-2">
        <BreakdownPanel title={t('vas.applications.byVisaType')} nameHeader={t('filters.visaType')} rows={report.visaTypes} />
        <BreakdownPanel
          title={t('vas.outcomes.byVisaType')}
          nameHeader={t('filters.visaType')}
          rows={report.visaTypes}
          toBars={toApprovalRateBars}
          max={100}
        />
      </div>

      <Panel title={t('vas.visaTypes.tableTitle')}>
        <DataTable
          caption={t('vas.visaTypes.tableTitle')}
          columns={columns}
          rows={report.visaTypes}
          rowKey={(row) => row.key}
        />
      </Panel>
    </Stagger>
  );
}
