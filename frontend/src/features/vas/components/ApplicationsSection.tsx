import { t } from '@/core/i18n';
import { DataTable } from '@/shared/components/DataTable';
import { KpiCard } from '@/shared/components/KpiCard';
import { ChartPanel, Stagger, StaggerItem } from '@/shared/components/Panel';
import { BreakdownPanel } from '@/shared/components/ReportBody';
import { TrendChart } from '@/shared/components/TrendChart';
import { SERIES_SLOT, toTrendData } from '@/shared/utils/chartMappers';
import { formatNumber, formatPeriod } from '@/shared/utils/format';
import type { VasApplications } from '../api/vasRepository';

/** Visa applications and volumes: received, processed and pending, with trend and breakdowns. */
export function ApplicationsSection({ report }: { report: VasApplications }) {
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

      <ChartPanel
        title={t('vas.applications.trendTitle')}
        chart={
          <TrendChart
            ariaLabel={t('vas.applications.trendTitle')}
            series={[
              { key: 'received', label: t('series.received'), slot: SERIES_SLOT.received },
              { key: 'decided', label: t('series.decided'), slot: SERIES_SLOT.decided },
            ]}
            data={toTrendData(trend.points, trend.granularity, (point) => ({
              received: point.received,
              decided: point.decided,
            }))}
          />
        }
        table={
          <DataTable
            caption={t('vas.applications.trendTitle')}
            rows={trend.points}
            rowKey={(point) => point.periodStart}
            columns={[
              { key: 'period', header: t('chart.period'), render: (point) => formatPeriod(point.periodStart, trend.granularity) },
              { key: 'received', header: t('series.received'), numeric: true, render: (point) => formatNumber(point.received) },
              { key: 'decided', header: t('series.decided'), numeric: true, render: (point) => formatNumber(point.decided) },
            ]}
          />
        }
      />

      <div className="grid-3">
        <BreakdownPanel title={t('vas.applications.byCountry')} nameHeader={t('column.country')} rows={report.byCountry} />
        <BreakdownPanel title={t('vas.applications.byVisaType')} nameHeader={t('filters.visaType')} rows={report.byVisaType} />
        <BreakdownPanel title={t('vas.applications.byChannel')} nameHeader={t('column.channel')} rows={report.byChannel} />
      </div>
    </Stagger>
  );
}
