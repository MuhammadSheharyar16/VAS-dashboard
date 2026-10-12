import { Clock, FlaskConical, Info } from 'lucide-react';
import { t } from '@/core/i18n';
import type { ReportMeta } from '@/shared/models/reporting';
import { formatDate, formatDateTimeUtc } from '@/shared/utils/format';
import { addDays } from '@/core/sanitizers/filters';

interface PageHeaderProps {
  title: string;
  subtitle: string;
  /** Present once a report has loaded. It says what the page is showing and how fresh it is. */
  meta?: ReportMeta;
}

export function MockBadge() {
  return (
    <span className="badge badge--mock" title={t('topbar.mockBadgeHint')}>
      <FlaskConical size={13} aria-hidden="true" />
      {t('topbar.mockBadge')}
      <span className="visually-hidden">{t('topbar.mockBadgeHint')}</span>
    </span>
  );
}

/** Title, context, and the facts about the data on the page: its period, its freshness and its source. */
export function PageHeader({ title, subtitle, meta }: PageHeaderProps) {
  return (
    <header className="page-header">
      <div>
        <h1 className="page-header__title">{title}</h1>
        <p className="page-header__subtitle">{subtitle}</p>
      </div>

      {meta && (
        <div className="page-header__meta">
          {meta.dataSource === 'Mock' && <MockBadge />}
          <span>
            {formatDate(meta.filter.from)} – {formatDate(addDays(meta.filter.toExclusive, -1))}
          </span>
          <span className="legend__item">
            <Clock size={13} aria-hidden="true" />
            {meta.freshness.lastRefreshedAtUtc
              ? t('topbar.freshness', { time: formatDateTimeUtc(meta.freshness.lastRefreshedAtUtc) })
              : t('topbar.freshnessUnknown')}
          </span>
          {meta.thresholdsArePlaceholders && (
            <span className="legend__item">
              <Info size={13} aria-hidden="true" />
              {t('topbar.placeholderThresholds')}
            </span>
          )}
        </div>
      )}
    </header>
  );
}
