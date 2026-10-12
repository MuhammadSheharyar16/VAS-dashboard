import { tOr } from '@/core/i18n';
import type { BarItem } from '@/shared/components/BarBreakdown';
import type { DonutSlice } from '@/shared/components/DonutShare';
import type { TrendDatum } from '@/shared/components/TrendChart';
import type { BreakdownRow, CountShare, OutcomeShare, TrendGranularity } from '@/shared/models/reporting';
import { formatDays, formatNumber, formatPercent, formatPeriod } from './format';

// These turn report rows into what a chart draws. They only pick, label and format values the
// API already calculated; none of them computes a KPI.

/** One chart point per period, with a readable label for the axis and the tooltip. */
export function toTrendData<T extends { periodStart: string }>(
  points: T[],
  granularity: TrendGranularity,
  pick: (point: T) => Record<string, number>,
): TrendDatum[] {
  return points.map((point) => ({ label: formatPeriod(point.periodStart, granularity), ...pick(point) }));
}

export function toVolumeBars(rows: BreakdownRow[], limit = 8): BarItem[] {
  return rows.slice(0, limit).map((row) => ({
    key: row.key,
    name: tOr(`channel.${row.name}`, row.name),
    value: row.received,
    valueLabel: formatNumber(row.received),
    note: formatPercent(row.sharePercent),
  }));
}

/** Approval rate per row, as bars out of 100. Rows with no decided applications are left out. */
export function toApprovalRateBars(rows: BreakdownRow[], limit = 8): BarItem[] {
  return rows
    .filter((row) => row.approvalRate !== null)
    .slice(0, limit)
    .map((row) => ({
      key: row.key,
      name: row.name,
      value: row.approvalRate ?? 0,
      valueLabel: formatPercent(row.approvalRate),
    }));
}

/** Average turnaround per row. Rows with no decided applications are left out. */
export function toTurnaroundBars(rows: BreakdownRow[], limit = 8): BarItem[] {
  return rows
    .filter((row) => row.averageTurnaroundDays !== null)
    .slice(0, limit)
    .map((row) => ({
      key: row.key,
      name: tOr(`channel.${row.name}`, row.name),
      value: row.averageTurnaroundDays ?? 0,
      valueLabel: formatDays(row.averageTurnaroundDays),
    }));
}

/**
 * The colour slot of each series, fixed for the whole app: "Approved" is the same colour on every
 * chart, whatever else is drawn beside it.
 */
export const SERIES_SLOT = {
  received: 1,
  revenue: 1,
  decided: 7,
  approved: 3,
  rejected: 2,
  withdrawn: 4,
  pending: 7,
} as const;

const OUTCOME_SLOT: Record<string, number> = {
  Approved: SERIES_SLOT.approved,
  Rejected: SERIES_SLOT.rejected,
  Withdrawn: SERIES_SLOT.withdrawn,
  Pending: SERIES_SLOT.pending,
};

export function toOutcomeSlices(outcomes: OutcomeShare[]): DonutSlice[] {
  return outcomes.map((outcome) => ({
    key: outcome.outcome,
    label: tOr(`outcome.${outcome.outcome}`, outcome.outcome),
    value: outcome.count,
    sharePercent: outcome.sharePercent,
    slot: OUTCOME_SLOT[outcome.outcome],
  }));
}

export function toCountBars(counts: CountShare[], labelPrefix: 'outcome' | 'stage'): BarItem[] {
  return counts.map((count) => ({
    key: count.key,
    name: tOr(`${labelPrefix}.${count.key}`, count.key),
    value: count.count,
    valueLabel: formatNumber(count.count),
    note: formatPercent(count.sharePercent),
  }));
}
