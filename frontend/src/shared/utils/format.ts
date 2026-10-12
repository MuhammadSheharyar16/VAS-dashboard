import { t } from '@/core/i18n';
import type { KpiCard, TrendGranularity } from '@/shared/models/reporting';

const LOCALE = 'en-GB';

const integer = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 0 });
const oneDecimal = new Intl.NumberFormat(LOCALE, { minimumFractionDigits: 1, maximumFractionDigits: 1 });
const compact = new Intl.NumberFormat(LOCALE, { notation: 'compact', maximumFractionDigits: 1 });
const dayLabel = new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'short', timeZone: 'UTC' });
const monthLabel = new Intl.DateTimeFormat(LOCALE, { month: 'short', year: 'numeric', timeZone: 'UTC' });
const fullDate = new Intl.DateTimeFormat(LOCALE, { day: 'numeric', month: 'short', year: 'numeric', timeZone: 'UTC' });
const dateTime = new Intl.DateTimeFormat(LOCALE, {
  day: 'numeric',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  timeZone: 'UTC',
});

export const NOT_AVAILABLE = '–';

export function formatNumber(value: number | null | undefined): string {
  return value === null || value === undefined ? NOT_AVAILABLE : integer.format(value);
}

export function formatCompact(value: number): string {
  return compact.format(value);
}

/** The API sends percentages as numbers out of 100. */
export function formatPercent(value: number | null | undefined): string {
  return value === null || value === undefined ? NOT_AVAILABLE : `${oneDecimal.format(value)}%`;
}

export function formatDays(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return NOT_AVAILABLE;
  }

  return t(value === 1 ? 'unit.day' : 'unit.days', { value: oneDecimal.format(value) });
}

export function formatCurrency(amount: number | null | undefined, currency: string | null, compactForm = false): string {
  if (amount === null || amount === undefined) {
    return NOT_AVAILABLE;
  }

  if (!currency) {
    return integer.format(amount);
  }

  return new Intl.NumberFormat(LOCALE, {
    style: 'currency',
    currency,
    currencyDisplay: 'code',
    notation: compactForm ? 'compact' : 'standard',
    maximumFractionDigits: compactForm ? 1 : 0,
  }).format(amount);
}

/** Formats a raw number the way its KPI unit is shown. Used for the card value and its count-up. */
export function formatByUnit(value: number | null, unit: KpiCard['unit'], currency: string | null): string {
  switch (unit) {
    case 'Percent':
      return formatPercent(value);
    case 'Days':
      return formatDays(value);
    case 'Currency':
      return formatCurrency(value, currency);
    default:
      return formatNumber(value);
  }
}

export function formatChange(changePercent: number | null): string {
  if (changePercent === null) {
    return NOT_AVAILABLE;
  }

  return `${oneDecimal.format(Math.abs(changePercent))}%`;
}

function parseDate(isoDate: string): Date {
  return new Date(`${isoDate.slice(0, 10)}T00:00:00Z`);
}

export function formatPeriod(periodStart: string, granularity: TrendGranularity): string {
  return (granularity === 'Month' ? monthLabel : dayLabel).format(parseDate(periodStart));
}

export function formatDate(isoDate: string): string {
  return fullDate.format(parseDate(isoDate));
}

export function formatDateTimeUtc(isoDateTime: string): string {
  return dateTime.format(new Date(isoDateTime));
}
