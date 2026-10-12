// The limits come from project-specs.md section 2.1. The server applies the same ones and is authoritative.

export const MAX_CODE_LENGTH = 20;
export const MAX_RANGE_MONTHS = 24;

const CODE_PATTERN = /^[A-Za-z0-9_-]+$/;
const DATE_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

/** Returns the code when it is letters, digits, `-` or `_` and at most 20 characters; otherwise null. */
export function sanitizeCode(value: string | null | undefined): string | null {
  const trimmed = value?.trim() ?? '';
  return trimmed.length > 0 && trimmed.length <= MAX_CODE_LENGTH && CODE_PATTERN.test(trimmed) ? trimmed : null;
}

/** Returns the date when it is a real calendar date written as yyyy-MM-dd; otherwise null. */
export function sanitizeDate(value: string | null | undefined): string | null {
  const match = DATE_PATTERN.exec(value?.trim() ?? '');
  if (!match) {
    return null;
  }

  const [year, month, day] = [Number(match[1]), Number(match[2]), Number(match[3])];
  const date = new Date(Date.UTC(year, month - 1, day));
  const isReal = date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;

  return isReal ? match[0] : null;
}

export function addDays(isoDate: string, days: number): string {
  const date = new Date(`${isoDate}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

function addMonths(isoDate: string, months: number): string {
  const date = new Date(`${isoDate}T00:00:00Z`);
  date.setUTCMonth(date.getUTCMonth() + months);
  return date.toISOString().slice(0, 10);
}

export type RangeProblem = 'order' | 'tooLong';

/** Checks a half-open range: `from` is included, `toExclusive` is not. */
export function validateRange(from: string, toExclusive: string): RangeProblem | null {
  if (from >= toExclusive) {
    return 'order';
  }

  return addMonths(from, MAX_RANGE_MONTHS) < toExclusive ? 'tooLong' : null;
}
