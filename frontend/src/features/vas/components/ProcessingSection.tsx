import { t, tOr } from '@/core/i18n';
import { BarBreakdown } from '@/shared/components/BarBreakdown';
import { DataTable } from '@/shared/components/DataTable';
import { KpiCard } from '@/shared/components/KpiCard';
import { ChartPanel, Panel, Stagger, StaggerItem } from '@/shared/components/Panel';
import { BreakdownPanel } from '@/shared/components/ReportBody';
import { Legend } from '@/shared/components/TrendChart';
import { toCountBars, toTurnaroundBars } from '@/shared/utils/chartMappers';
import { formatNumber, formatPercent } from '@/shared/utils/format';
import type { VasProcessing } from '../api/vasRepository';

/** Visa processing and journey: status, stages, the digital and centre journeys, and turnaround. */
export function ProcessingSection({ report }: { report: VasProcessing }) {
  const stageName = (stage: string) => tOr(`stage.${stage}`, stage);

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
        title={t('vas.processing.funnelTitle')}
        subtitle={t('vas.processing.funnelSubtitle')}
        chart={
          <>
            <Legend
              series={[
                { key: 'reached', label: t('series.reached'), slot: 1 },
                { key: 'completed', label: t('series.completed'), slot: 3 },
              ]}
            />
            <BarBreakdown
              ariaLabel={t('vas.processing.funnelTitle')}
              items={report.stageFunnel.map((stage) => ({
                key: stage.stage,
                name: stageName(stage.stage),
                value: stage.reached,
                secondaryValue: stage.completed,
                valueLabel: formatNumber(stage.reached),
                note: formatPercent(stage.completionPercent),
              }))}
            />
          </>
        }
        table={
          <DataTable
            caption={t('vas.processing.funnelTitle')}
            rows={report.stageFunnel}
            rowKey={(stage) => stage.stage}
            columns={[
              { key: 'stage', header: t('column.stage'), render: (stage) => stageName(stage.stage) },
              { key: 'reached', header: t('column.reached'), numeric: true, render: (stage) => formatNumber(stage.reached) },
              { key: 'completed', header: t('column.completed'), numeric: true, render: (stage) => formatNumber(stage.completed) },
              { key: 'completion', header: t('column.completion'), numeric: true, render: (stage) => formatPercent(stage.completionPercent) },
            ]}
          />
        }
      />

      <div className="grid-2">
        <Panel title={t('vas.processing.statusTitle')}>
          <BarBreakdown ariaLabel={t('vas.processing.statusTitle')} items={toCountBars(report.statuses, 'outcome')} />
        </Panel>
        <Panel title={t('vas.processing.pendingTitle')}>
          <BarBreakdown ariaLabel={t('vas.processing.pendingTitle')} items={toCountBars(report.pendingByStage, 'stage')} />
        </Panel>
      </div>

      <div className="grid-2">
        <BreakdownPanel
          title={t('vas.processing.channelTitle')}
          nameHeader={t('column.channel')}
          rows={report.channels}
          toBars={toTurnaroundBars}
        />
        <BreakdownPanel
          title={t('vas.processing.turnaroundTitle')}
          nameHeader={t('filters.visaType')}
          rows={report.turnaroundByVisaType}
          toBars={toTurnaroundBars}
        />
      </div>
    </Stagger>
  );
}
