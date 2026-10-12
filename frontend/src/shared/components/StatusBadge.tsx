import { AlertTriangle, CheckCircle2, CircleDashed, OctagonAlert, type LucideIcon } from 'lucide-react';
import { t } from '@/core/i18n';
import type { KpiStatus } from '@/shared/models/reporting';

/** The one place a status becomes a label, an icon and a colour. Never colour alone. */
const STATUS_ICON: Record<KpiStatus, LucideIcon> = {
  Good: CheckCircle2,
  Warning: AlertTriangle,
  Critical: OctagonAlert,
  Neutral: CircleDashed,
};

export function StatusBadge({ status }: { status: KpiStatus }) {
  const Icon = STATUS_ICON[status];

  return (
    <span className={`badge badge--${status}`}>
      <Icon size={13} aria-hidden="true" />
      {t(`status.${status}`)}
    </span>
  );
}
