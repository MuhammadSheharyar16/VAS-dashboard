import { Inbox, RotateCw, SearchX, TriangleAlert } from 'lucide-react';
import type { CSSProperties } from 'react';
import { t } from '@/core/i18n';
import { ApiError } from '@/core/http/apiClient';

export function Skeleton({ height, width = '100%' }: { height: number | string; width?: number | string }) {
  const style: CSSProperties = { blockSize: height, inlineSize: width };
  return <div className="skeleton" style={style} aria-hidden="true" />;
}

/** The loading layout of a report: the same blocks the content will fill, so nothing jumps. */
export function ReportSkeleton({ cards = 6 }: { cards?: number }) {
  return (
    <div role="status" aria-live="polite" className="report-stack">
      <span className="visually-hidden">{t('state.loading')}</span>
      <div className="kpi-grid">
        {Array.from({ length: cards }, (_, index) => (
          <Skeleton key={index} height={168} />
        ))}
      </div>
      <Skeleton height={340} />
      <Skeleton height={280} />
    </div>
  );
}

interface EmptyStateProps {
  /** "noData": nothing exists for the selection. "noMatches": rows exist but the filters hide them all. */
  variant?: 'noData' | 'noMatches';
}

export function EmptyState({ variant = 'noData' }: EmptyStateProps) {
  const isNoMatches = variant === 'noMatches';
  const Icon = isNoMatches ? SearchX : Inbox;

  return (
    <div className="state" role="status">
      <span className="state__icon" aria-hidden="true">
        <Icon size={26} />
      </span>
      <h2 className="state__title">{t(isNoMatches ? 'state.noMatchesTitle' : 'state.emptyTitle')}</h2>
      <p className="state__body">{t(isNoMatches ? 'state.noMatchesBody' : 'state.emptyBody')}</p>
    </div>
  );
}

function errorBodyKey(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.kind) {
      case 'network':
        return 'state.errorNetwork';
      case 'rateLimited':
        return 'state.errorRateLimited';
      case 'validation':
        return 'state.errorValidation';
      default:
        return 'state.errorBody';
    }
  }

  return 'state.errorBody';
}

interface ErrorStateProps {
  error: unknown;
  onRetry(): void;
}

/** A stable message for the user. Server details are never shown; the reference lets support find the log. */
export function ErrorState({ error, onRetry }: ErrorStateProps) {
  const traceId = error instanceof ApiError ? error.traceId : null;

  return (
    <div className="state" role="alert">
      <span className="state__icon state__icon--error" aria-hidden="true">
        <TriangleAlert size={26} />
      </span>
      <h2 className="state__title">{t('state.errorTitle')}</h2>
      <p className="state__body">{t(errorBodyKey(error))}</p>
      {traceId && <p className="state__trace">{t('state.traceId', { traceId })}</p>}
      <button type="button" className="button button--primary" onClick={onRetry}>
        <RotateCw size={15} aria-hidden="true" />
        {t('state.retry')}
      </button>
    </div>
  );
}
