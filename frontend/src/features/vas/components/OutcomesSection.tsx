import { t } from '@/core/i18n';
import { DataTable } from '@/shared/components/DataTable';
import { DonutShare } from '@/shared/components/DonutShare';
import { KpiCard } from '@/shared/components/KpiCard';
import { ChartPanel, Panel, Stagger, StaggerItem } from '@/shared/components/Panel';
import { BreakdownPanel } from '@/shared/components/ReportBody';
import { TrendChart } from '@/shared/components/TrendChart';
import { SERIES_SLOT, toApprovalRateBars, toOutcomeSlices, toTrendData } from '@/shared/utils/chartMappers';
import { formatNumber, formatPeriod } from '@/shared/utils/format';
import type { VasOutcomes } from '../api/vasRepository';

/** Visa outcomes: approved, rejected and withdrawn, with approval and rejection rates. */
export function OutcomesSection({ report }: { report: VasOutcomes }) {
  const { trend } = report;

  return (
    <Stagger className="report-stack">
      <div className="kpi-grid">
        {report.kpis.map((kpi) => (
          <StaggerItem key={kpi.key}>
            <KpiCard kpi={kpi} />
          </StaggerItem>
        ))}
      </div>

      <div className="grid-2-1">
        <ChartPanel
          title={t('vas.outcomes.trendTitle')}
          chart={
            <TrendChart
              ariaLabel={t('vas.outcomes.trendTitle')}
              series={[
                { key: 'approved', label: t('series.approved'), slot: SERIES_SLOT.approved },
                { key: 'rejected', label: t('series.rejected'), slot: SERIES_SLOT.rejected },
                { key: 'withdrawn', label: t('series.withdrawn'), slot: SERIES_SLOT.withdrawn },
              ]}
              data={toTrendData(trend.points, trend.granularity, (point) => ({
                approved: point.approved,
                rejected: point.rejected,
                withdrawn: point.withdrawn,
              }))}
            />
          }
          table={
            <DataTable
              caption={t('vas.outcomes.trendTitle')}
              rows={trend.points}
              rowKey={(point) => point.periodStart}
              columns={[
                { key: 'period', header: t('chart.period'), render: (point) => formatPeriod(point.periodStart, trend.granularity) },
                { key: 'approved', header: t('series.approved'), numeric: true, render: (point) => formatNumber(point.approved) },
                { key: 'rejected', header: t('series.rejected'), numeric: true, render: (point) => formatNumber(point.rejected) },
                { key: 'withdrawn', header: t('series.withdrawn'), numeric: true, render: (point) => formatNumber(point.withdrawn) },
              ]}
            />
          }
        />

        <Panel title={t('vas.outcomes.mixTitle')}>
          <DonutShare ariaLabel={t('vas.outcomes.mixTitle')} slices={toOutcomeSlices(report.outcomes)} />
        </Panel>
      </div>

      <div className="grid-2">
        <BreakdownPanel
          title={t('vas.outcomes.byVisaType')}
          nameHeader={t('filters.visaType')}
          rows={report.byVisaType}
          toBars={toApprovalRateBars}
          max={100}
        />
        <BreakdownPanel
          title={t('vas.outcomes.byCountry')}
          nameHeader={t('column.country')}
          rows={report.byCountry}
          toBars={toApprovalRateBars}
          max={100}
        />
      </div>
    </Stagger>
  );
}
