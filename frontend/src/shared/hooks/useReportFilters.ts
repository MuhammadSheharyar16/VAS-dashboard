import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router-dom';
import { addDays, sanitizeCode, sanitizeDate, validateRange, type RangeProblem } from '@/core/sanitizers/filters';
import type { ReportFilters } from '@/shared/models/reporting';

export const PERIOD_PRESETS = [7, 30, 90, 365] as const;
export const DEFAULT_PERIOD = 30;

const PARAM = {
  period: 'period',
  from: 'from',
  to: 'to',
  country: 'country',
  centre: 'centre',
  visaType: 'visaType',
  compare: 'compare',
} as const;

export interface ReportFilterState {
  filters: ReportFilters;
  /** Set when a custom range is chosen but is not usable; the report is not requested until it is fixed. */
  problem: RangeProblem | 'incomplete' | null;
  /** The custom range as the user typed it; `to` is the last day included. */
  customRange: { from: string; to: string };
  isCustom: boolean;
  setPeriod(days: number | 'custom'): void;
  setCustomRange(range: { from?: string; to?: string }): void;
  setCountry(code: string | null): void;
  setCentre(code: string | null): void;
  setVisaType(code: string | null): void;
  setCompare(compare: boolean): void;
  reset(): void;
  isDefault: boolean;
}

/**
 * Parses the filters out of a query string. Everything passes through a sanitiser here, so a
 * hand-edited URL can never put an unchecked value into an API request.
 */
export function parseFilters(params: URLSearchParams): Pick<ReportFilterState, 'filters' | 'problem' | 'customRange' | 'isCustom'> {
  const isCustom = params.get(PARAM.period) === 'custom';
  const presetValue = Number(params.get(PARAM.period));
  const lastDays = PERIOD_PRESETS.find((preset) => preset === presetValue) ?? DEFAULT_PERIOD;

  const from = sanitizeDate(params.get(PARAM.from));
  const to = sanitizeDate(params.get(PARAM.to));
  const toExclusive = to ? addDays(to, 1) : null;

  let problem: ReportFilterState['problem'] = null;
  if (isCustom) {
    problem = from && toExclusive ? validateRange(from, toExclusive) : 'incomplete';
  }

  return {
    isCustom,
    customRange: { from: from ?? '', to: to ?? '' },
    problem,
    filters: {
      lastDays: isCustom ? null : lastDays,
      from: isCustom ? from : null,
      toExclusive: isCustom ? toExclusive : null,
      countryCode: sanitizeCode(params.get(PARAM.country)),
      centreCode: sanitizeCode(params.get(PARAM.centre)),
      visaTypeCode: sanitizeCode(params.get(PARAM.visaType)),
      compare: params.get(PARAM.compare) !== '0',
    },
  };
}

/** The report filters live in the URL, so a view can be bookmarked, shared and restored on refresh. */
export function useReportFilters(): ReportFilterState {
  const [params, setParams] = useSearchParams();

  const update = useCallback(
    (changes: Record<string, string | null>) => {
      setParams(
        (current) => {
          const next = new URLSearchParams(current);
          for (const [name, value] of Object.entries(changes)) {
            if (value === null || value === '') {
              next.delete(name);
            } else {
              next.set(name, value);
            }
          }

          return next;
        },
        { replace: true },
      );
    },
    [setParams],
  );

  const parsed = useMemo(() => parseFilters(params), [params]);

  return useMemo<ReportFilterState>(() => {
    const filterKeys = Object.values(PARAM);

    return {
      ...parsed,
      isDefault: filterKeys.every((key) => !params.has(key)),
      setPeriod: (days) =>
        update(
          days === 'custom'
            ? { [PARAM.period]: 'custom' }
            : { [PARAM.period]: days === DEFAULT_PERIOD ? null : String(days), [PARAM.from]: null, [PARAM.to]: null },
        ),
      setCustomRange: (range) =>
        update({
          ...(range.from !== undefined ? { [PARAM.from]: sanitizeDate(range.from) } : {}),
          ...(range.to !== undefined ? { [PARAM.to]: sanitizeDate(range.to) } : {}),
        }),
      // Changing the country clears the centre, because a centre belongs to one country.
      setCountry: (code) => update({ [PARAM.country]: sanitizeCode(code), [PARAM.centre]: null }),
      setCentre: (code) => update({ [PARAM.centre]: sanitizeCode(code) }),
      setVisaType: (code) => update({ [PARAM.visaType]: sanitizeCode(code) }),
      setCompare: (compare) => update({ [PARAM.compare]: compare ? null : '0' }),
      reset: () => update(Object.fromEntries(filterKeys.map((key) => [key, null]))),
    };
  }, [parsed, params, update]);
}
