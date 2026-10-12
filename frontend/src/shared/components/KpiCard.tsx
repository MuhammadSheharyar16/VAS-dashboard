import {
  ArrowDownRight,
  ArrowRight,
  ArrowUpRight,
  Banknote,
  CheckCheck,
  CircleSlash,
  Clock3,
  FileInput,
  Gauge,
  Hourglass,
  MonitorSmartphone,
  Receipt,
  Undo2,
  type LucideIcon,
} from 'lucide-react';
import { useCallback } from 'react';
import { t, tOr } from '@/core/i18n';
import type { KpiCard as KpiCardModel, TrendDirection } from '@/shared/models/reporting';
import { formatByUnit, formatChange, formatNumber, NOT_AVAILABLE } from '@/shared/utils/format';
import { AnimatedNumber } from './AnimatedNumber';
import { StatusBadge } from './StatusBadge';
import { TiltCard } from './TiltCard';

const KPI_ICON: Record<string, LucideIcon> = {
  applicationsReceived: FileInput,
  applicationsProcessed: CheckCheck,
  applicationsPending: Hourglass,
  applicationsApproved: CheckCheck,
  applicationsRejected: CircleSlash,
  applicationsWithdrawn: Undo2,
  approvalRate: Gauge,
  rejectionRate: Gauge,
  averageTurnaroundDays: Clock3,
  digitalTurnaroundDays: MonitorSmartphone,
  centreTurnaroundDays: Clock3,
  digitalSharePercent: MonitorSmartphone,
  vasRevenue: Banknote,
  revenueTransactions: Receipt,
};

const TREND_ICON: Record<TrendDirection, LucideIcon> = {
  Up: ArrowUpRight,
  Down: ArrowDownRight,
  Flat: ArrowRight,
};

const TREND_TEXT: Record<TrendDirection, string> = {
  Up: 'kpi.changeUp',
  Down: 'kpi.changeDown',
  Flat: 'kpi.changeFlat',
};

/**
 * One KPI as the API sent it: the value, how it moved, and its status.
 * Whether a rise is good or bad is the API's call (the status), so the change chip stays neutral.
 */
export function KpiCard({ kpi }: { kpi: KpiCardModel }) {
  const Icon = KPI_ICON[kpi.key] ?? Gauge;
  const TrendIcon = TREND_ICON[kpi.trend];
  const label = tOr(`kpi.${kpi.key}`, kpi.key);

  // Currency cards show the code beside the number, so the count-up animates a plain amount.
  const isCurrency = kpi.unit === 'Currency';
  const format = useCallback(
    (value: number | null) => (isCurrency ? formatNumber(value) : formatByUnit(value, kpi.unit, kpi.currency)),
    [isCurrency, kpi.unit, kpi.currency],
  );

  const hasComparison = kpi.previousValue !== null;
  const changeText = kpi.changePercent === null ? NOT_AVAILABLE : formatChange(kpi.changePercent);

  return (
    <TiltCard>
      <article className="kpi" aria-label={label}>
        <div className="kpi__top">
          <span className="kpi__icon" aria-hidden="true">
            <Icon size={19} />
          </span>
          {kpi.status !== 'Neutral' && <StatusBadge status={kpi.status} />}
        </div>

        <h3 className="kpi__label">{label}</h3>

        <p className="kpi__value">
          <AnimatedNumber value={kpi.value} format={format} />
          {isCurrency && kpi.currency && <span className="kpi__currency">{kpi.currency}</span>}
        </p>

        <div className="kpi__footer">
          {hasComparison ? (
            <>
              <span className="kpi__change" title={t(TREND_TEXT[kpi.trend], { value: changeText })}>
                <TrendIcon size={14} aria-hidden="true" />
                <span className="visually-hidden">{t(TREND_TEXT[kpi.trend], { value: changeText })}</span>
                <span aria-hidden="true">{changeText}</span>
              </span>
              <span>{t('kpi.previous', { value: formatByUnit(kpi.previousValue, kpi.unit, kpi.currency) })}</span>
            </>
          ) : (
            <span>{t('kpi.noComparison')}</span>
          )}
        </div>
      </article>
    </TiltCard>
  );
}
