import { useId, useState } from 'react';
import { t } from '@/core/i18n';
import { BarBreakdown } from '@/shared/components/BarBreakdown';
import { DataTable } from '@/shared/components/DataTable';
import { KpiCard } from '@/shared/components/KpiCard';
import { ChartPanel, Stagger, StaggerItem } from '@/shared/components/Panel';
import { Segmented } from '@/shared/components/Segmented';
import { TrendChart } from '@/shared/components/TrendChart';
import { SERIES_SLOT, toTrendData } from '@/shared/utils/chartMappers';
import { formatCurrency, formatNumber, formatPercent, formatPeriod } from '@/shared/utils/format';
import type { RevenueBreakdownRow, VasRevenue } from '../api/vasRepository';

interface RevenueBreakdownProps {
  title: string;
  nameHeader: string;
  rows: RevenueBreakdownRow[];
  currency: string;
}

function RevenueBreakdown({ title, nameHeader, rows, currency }: RevenueBreakdownProps) {
  const inCurrency = rows.filter((row) => row.currency === currency);

  return (
    <ChartPanel
      title={title}
      chart={
        <BarBreakdown
          ariaLabel={title}
          items={inCurrency.slice(0, 8).map((row) => ({
            key: row.key,
            name: row.name,
            value: row.amount,
            valueLabel: formatCurrency(row.amount, currency, true),
            note: formatPercent(row.sharePercent),
          }))}
        />
      }
      table={
        <DataTable
          caption={title}
          rows={inCurrency}
          rowKey={(row) => row.key}
          columns={[
            { key: 'name', header: nameHeader, render: (row) => row.name },
            { key: 'amount', header: t('column.amount'), numeric: true, render: (row) => formatCurrency(row.amount, currency) },
            { key: 'transactions', header: t('column.transactions'), numeric: true, render: (row) => formatNumber(row.transactions) },
            { key: 'share', header: t('column.share'), numeric: true, render: (row) => formatPercent(row.sharePercent) },
          ]}
        />
      }
    />
  );
}

/**
 * VAS revenue. Currencies are never added together, so when a period has more than one the
 * section shows one at a time.
 */
export function RevenueSection({ report }: { report: VasRevenue }) {
  const regionId = useId();
  const currencies = [...new Set(report.byService.map((row) => row.currency))];
  const [selected, setSelected] = useState<string | null>(null);
  const currency = selected && currencies.includes(selected) ? selected : (currencies[0] ?? '');

  const { trend } = report;
  const points = trend.points.filter((point) => point.currency === currency);

  return (
    <Stagger className="report-stack">
      <div className="kpi-grid">
        {report.kpis.map((kpi) => (
          <StaggerItem key={`${kpi.key}-${kpi.currency ?? ''}`}>
            <KpiCard kpi={kpi} />
          </StaggerItem>
        ))}
      </div>

      {currencies.length > 1 && (
        <Segmented
          label={t('column.amount')}
          controls={regionId}
          value={currency}
          onChange={setSelected}
          options={currencies.map((code) => ({ value: code, label: code }))}
        />
      )}

      <div id={regionId} className="report-stack">
        <ChartPanel
          title={t('vas.revenue.trendTitle')}
          subtitle={currency}
          chart={
            <TrendChart
              ariaLabel={t('vas.revenue.trendTitle')}
              series={[{ key: 'amount', label: t('series.revenue'), slot: SERIES_SLOT.revenue }]}
              formatValue={(value) => formatCurrency(value, currency)}
              data={toTrendData(points, trend.granularity, (point) => ({ amount: point.amount }))}
            />
          }
          table={
            <DataTable
              caption={t('vas.revenue.trendTitle')}
              rows={points}
              rowKey={(point) => point.periodStart}
              columns={[
                { key: 'period', header: t('chart.period'), render: (point) => formatPeriod(point.periodStart, trend.granularity) },
                { key: 'amount', header: t('column.amount'), numeric: true, render: (point) => formatCurrency(point.amount, currency) },
                { key: 'transactions', header: t('column.transactions'), numeric: true, render: (point) => formatNumber(point.transactions) },
              ]}
            />
          }
        />

        <div className="grid-2">
          <RevenueBreakdown title={t('vas.revenue.byService')} nameHeader={t('column.service')} rows={report.byService} currency={currency} />
          <RevenueBreakdown title={t('vas.revenue.byCountry')} nameHeader={t('column.country')} rows={report.byCountry} currency={currency} />
          <RevenueBreakdown title={t('vas.revenue.byVisaType')} nameHeader={t('filters.visaType')} rows={report.byVisaType} currency={currency} />
          <RevenueBreakdown title={t('vas.revenue.byCentre')} nameHeader={t('filters.centre')} rows={report.byCentre} currency={currency} />
        </div>
      </div>
    </Stagger>
  );
}
