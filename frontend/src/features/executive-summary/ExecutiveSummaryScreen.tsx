import { lazy, Suspense, useMemo } from 'react';
import { t } from '@/core/i18n';
import { useTheme } from '@/core/theme/ThemeProvider';
import { BarBreakdown } from '@/shared/components/BarBreakdown';
import { DataTable } from '@/shared/components/DataTable';
import { DonutShare } from '@/shared/components/DonutShare';
import { FilterBar } from '@/shared/components/FilterBar';
import { KpiCard } from '@/shared/components/KpiCard';
import { PageHeader } from '@/shared/components/PageHeader';
import { ChartPanel, Panel, Stagger, StaggerItem } from '@/shared/components/Panel';
import { ReportBody } from '@/shared/components/ReportBody';
import { Skeleton } from '@/shared/components/states';
import { TrendChart, type TrendSeries } from '@/shared/components/TrendChart';
import { SERIES_SLOT, toOutcomeSlices, toTrendData } from '@/shared/utils/chartMappers';
import { formatNumber, formatPercent, formatPeriod } from '@/shared/utils/format';
import type { CountryVolume, ExecutiveSummary } from './api/executiveSummaryRepository';
import { useExecutiveSummaryViewModel } from './useExecutiveSummaryViewModel';

// The 3D globe brings three.js with it, so it is only downloaded when this panel is shown.
const Globe3D = lazy(() => import('@/shared/components/Globe3D'));

function TopCountries({ countries }: { countries: CountryVolume[] }) {
  const { mode } = useTheme();
  const largest = Math.max(...countries.map((country) => country.received), 1);

  const markers = useMemo(
    () => countries.map((country) => ({ code: country.countryCode, weight: country.received / largest })),
    [countries, largest],
  );

  return (
    <Panel title={t('executiveSummary.countriesTitle')} subtitle={t('executiveSummary.countriesSubtitle')}>
      <div className="countries">
        <Suspense fallback={<Skeleton height={260} />}>
          <Globe3D markers={markers} themeKey={mode} ariaLabel={t('executiveSummary.globeLabel')} />
        </Suspense>
        <BarBreakdown
          ariaLabel={t('executiveSummary.countriesTitle')}
          items={countries.map((country) => ({
            key: country.countryCode,
            name: country.countryName,
            value: country.received,
            valueLabel: formatNumber(country.received),
            note: formatPercent(country.sharePercent),
          }))}
        />
      </div>
    </Panel>
  );
}

function Content({ summary }: { summary: ExecutiveSummary }) {
  const trend = summary.applicationsTrend;
  const series: TrendSeries[] = [
    { key: 'received', label: t('series.received'), slot: SERIES_SLOT.received },
    { key: 'decided', label: t('series.decided'), slot: SERIES_SLOT.decided },
  ];

  return (
    <Stagger className="report-stack">
      <div className="kpi-grid">
        {summary.kpis.map((kpi) => (
          <StaggerItem key={`${kpi.key}-${kpi.currency ?? ''}`}>
            <KpiCard kpi={kpi} />
          </StaggerItem>
        ))}
      </div>

      <div className="grid-2-1">
        <ChartPanel
          title={t('executiveSummary.trendTitle')}
          subtitle={t('executiveSummary.trendSubtitle')}
          chart={
            <TrendChart
              ariaLabel={t('executiveSummary.trendTitle')}
              series={series}
              data={toTrendData(trend.points, trend.granularity, (point) => ({
                received: point.received,
                decided: point.decided,
              }))}
            />
          }
          table={
            <DataTable
              caption={t('executiveSummary.trendTitle')}
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

        <Panel title={t('executiveSummary.outcomesTitle')} subtitle={t('executiveSummary.outcomesSubtitle')}>
          <DonutShare ariaLabel={t('executiveSummary.outcomesTitle')} slices={toOutcomeSlices(summary.outcomes)} />
        </Panel>
      </div>

      <TopCountries countries={summary.topCountries} />
    </Stagger>
  );
}

/** The Executive Summary screen. It only lays out what the view-model gives it. */
export function ExecutiveSummaryScreen() {
  const { filterState, lookups, report } = useExecutiveSummaryViewModel();
  const meta = report.status === 'content' || report.status === 'empty' ? report.data.meta : undefined;

  return (
    <div className="screen">
      <PageHeader title={t('executiveSummary.title')} subtitle={t('executiveSummary.subtitle')} meta={meta} />
      <FilterBar state={filterState} lookups={lookups} />
      <ReportBody state={report}>{(summary) => <Content summary={summary} />}</ReportBody>
    </div>
  );
}
