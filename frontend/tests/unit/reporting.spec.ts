import { describe, expect, it } from 'vitest';
import { canView } from '@/features/session/api/sessionRepository';
import type { CentreRow } from '@/features/vas/api/vasRepository';
import { isEmptyReport, isVasSection } from '@/features/vas/useVasViewModel';
import { parseFilters } from '@/shared/hooks/useReportFilters';
import { ringOffset } from '@/shared/components/Carousel3D';
import { toReportQuery, type BreakdownRow } from '@/shared/models/reporting';
import { carriedSearch } from '@/shared/utils/carriedFilters';
import { toApprovalRateBars, toOutcomeSlices, toTrendData, toVolumeBars } from '@/shared/utils/chartMappers';
import {
  formatByUnit,
  formatChange,
  formatCurrency,
  formatDateTimeUtc,
  formatDays,
  formatPercent,
  formatPeriod,
  NOT_AVAILABLE,
} from '@/shared/utils/format';

import { pageCentres } from '../support/centrePaging';
import { fixtureExecutiveSummaryRepository, fixtureVasRepository } from '../support/fixtureRepositories';

const parse = (query: string) => parseFilters(new URLSearchParams(query));

describe('carousel ring', () => {
  it('places each item the shortest way round from the front one', () => {
    expect([0, 1, 2, 3, 4, 5].map((index) => ringOffset(index, 0, 6))).toEqual([0, 1, 2, -3, -2, -1]);
    expect([0, 1, 2, 3, 4, 5].map((index) => ringOffset(index, 5, 6))).toEqual([1, 2, -3, -2, -1, 0]);
    expect([0, 1, 2].map((index) => ringOffset(index, 1, 3))).toEqual([-1, 0, 1]);
  });
});

describe('carried filters', () => {
  it('keeps the report filters and drops everything else', () => {
    expect(carriedSearch('?country=AE&section=revenue&period=90&page=2')).toBe('country=AE&period=90');
    expect(carriedSearch('')).toBe('');
  });
});

describe('filters from the URL', () => {
  it('defaults to the last 30 days with comparison on and nothing else set', () => {
    const { filters, problem, isCustom } = parse('');

    expect(filters).toEqual({
      lastDays: 30,
      from: null,
      toExclusive: null,
      countryCode: null,
      centreCode: null,
      visaTypeCode: null,
      compare: true,
    });
    expect(problem).toBeNull();
    expect(isCustom).toBe(false);
  });

  it('reads a preset, the codes and the comparison switch', () => {
    const { filters } = parse('period=90&country=AE&centre=AE-DXB&visaType=TOUR&compare=0');

    expect(filters).toMatchObject({ lastDays: 90, countryCode: 'AE', centreCode: 'AE-DXB', visaTypeCode: 'TOUR', compare: false });
  });

  it('falls back to 30 days for a period that is not a preset', () => {
    expect(parse('period=12345').filters.lastDays).toBe(30);
  });

  it('drops codes that are not safe instead of sending them', () => {
    const { filters } = parse(`country=${encodeURIComponent("AE' OR 1=1")}&visaType=${encodeURIComponent('<script>')}`);

    expect(filters.countryCode).toBeNull();
    expect(filters.visaTypeCode).toBeNull();
  });

  it('turns a custom range into a half-open range: the end day is included', () => {
    const { filters, problem } = parse('period=custom&from=2026-06-01&to=2026-06-10');

    expect(filters).toMatchObject({ lastDays: null, from: '2026-06-01', toExclusive: '2026-06-11' });
    expect(problem).toBeNull();
  });

  it.each([
    ['period=custom', 'incomplete'],
    ['period=custom&from=2026-06-01', 'incomplete'],
    ['period=custom&from=2026-06-10&to=2026-06-01', 'order'],
    ['period=custom&from=2024-01-01&to=2026-06-01', 'tooLong'],
    ['period=custom&from=not-a-date&to=2026-06-01', 'incomplete'],
  ])('flags %s as %s so no request is made', (query, expected) => {
    expect(parse(query).problem).toBe(expected);
  });

  it('sends a preset as lastDays and a custom range as dates, never both', () => {
    expect(toReportQuery(parse('period=7').filters)).toMatchObject({ lastDays: 7, from: null, toExclusive: null, compare: 'PreviousPeriod' });
    expect(toReportQuery(parse('period=custom&from=2026-06-01&to=2026-06-10&compare=0').filters)).toMatchObject({
      lastDays: null,
      from: '2026-06-01',
      toExclusive: '2026-06-11',
      compare: 'None',
    });
  });
});

describe('formatting', () => {
  it('formats each KPI unit', () => {
    expect(formatByUnit(1171, 'Count', null)).toBe('1,171');
    expect(formatByUnit(83.2, 'Percent', null)).toBe('83.2%');
    expect(formatByUnit(6.4, 'Days', null)).toBe('6.4 days');
    expect(formatByUnit(571700, 'Currency', 'AED')).toMatch(/AED\s?571,700/);
  });

  it('shows a dash for a value the API did not send', () => {
    expect(formatPercent(null)).toBe(NOT_AVAILABLE);
    expect(formatDays(null)).toBe(NOT_AVAILABLE);
    expect(formatCurrency(null, 'AED')).toBe(NOT_AVAILABLE);
    expect(formatChange(null)).toBe(NOT_AVAILABLE);
  });

  it('shows a change as a size without a sign, because the arrow carries the direction', () => {
    expect(formatChange(-13.6)).toBe('13.6%');
    expect(formatChange(2.3)).toBe('2.3%');
  });

  it('labels periods by day or by month, and times in UTC', () => {
    expect(formatPeriod('2026-09-27', 'Day')).toBe('27 Sept');
    expect(formatPeriod('2026-09-01', 'Month')).toBe('Sept 2026');
    expect(formatDateTimeUtc('2026-10-12T02:00:00Z')).toContain('02:00');
  });
});

const row = (overrides: Partial<BreakdownRow>): BreakdownRow => ({
  key: 'AE',
  name: 'United Arab Emirates',
  group: null,
  received: 10,
  processed: 8,
  pending: 2,
  approved: 6,
  rejected: 2,
  withdrawn: 0,
  approvalRate: 75,
  rejectionRate: 25,
  averageTurnaroundDays: 6,
  sharePercent: 50,
  ...overrides,
});

describe('chart mappers', () => {
  it('labels trend points and keeps the picked values', () => {
    const data = toTrendData([{ periodStart: '2026-09-27', received: 0, decided: 0 }], 'Day', (point) => ({
      received: point.received,
    }));

    expect(data).toEqual([{ label: '27 Sept', received: 0 }]);
  });

  it('turns breakdown rows into volume bars with the share as a note', () => {
    expect(toVolumeBars([row({})])).toEqual([
      { key: 'AE', name: 'United Arab Emirates', value: 10, valueLabel: '10', note: '50.0%' },
    ]);
  });

  it('leaves out rows without a rate from the approval-rate bars', () => {
    const bars = toApprovalRateBars([row({}), row({ key: 'STU', approvalRate: null })]);

    expect(bars.map((bar) => bar.key)).toEqual(['AE']);
    expect(bars[0]?.valueLabel).toBe('75.0%');
  });

  it('gives each outcome its fixed colour slot whatever order it arrives in', () => {
    const slices = toOutcomeSlices([
      { outcome: 'Pending', count: 1, sharePercent: 10 },
      { outcome: 'Approved', count: 9, sharePercent: 90 },
    ]);

    expect(slices.map((slice) => [slice.key, slice.slot])).toEqual([
      ['Pending', 7],
      ['Approved', 3],
    ]);
  });
});

describe('centre paging of the fixture backend', () => {
  const centre = (code: string, received: number, turnaround: number | null): CentreRow => ({
    centreCode: code,
    centreName: `${code} Centre`,
    countryCode: 'AE',
    countryName: 'United Arab Emirates',
    city: code,
    dailyCapacity: 6,
    received,
    processed: received,
    pending: 0,
    approvalRate: null,
    averageTurnaroundDays: turnaround,
    averageDailyApplications: 1,
    capacityUtilisationPercent: null,
    capacityStatus: 'Neutral',
  });

  const rows = [centre('B', 5, null), centre('A', 9, 4), centre('C', 1, 7)];

  it('sorts by the chosen field and direction', () => {
    const page = pageCentres(rows, { page: 1, pageSize: 10, sortBy: 'received', sortDirection: 'Descending' });

    expect(page.items.map((item) => item.centreCode)).toEqual(['A', 'B', 'C']);
  });

  it('keeps centres without a value last in either direction', () => {
    const ascending = pageCentres(rows, { page: 1, pageSize: 10, sortBy: 'averageTurnaroundDays', sortDirection: 'Ascending' });
    const descending = pageCentres(rows, { page: 1, pageSize: 10, sortBy: 'averageTurnaroundDays', sortDirection: 'Descending' });

    expect(ascending.items.map((item) => item.centreCode)).toEqual(['A', 'C', 'B']);
    expect(descending.items.map((item) => item.centreCode)).toEqual(['C', 'A', 'B']);
  });

  it('returns one page and the paging facts', () => {
    const page = pageCentres(rows, { page: 2, pageSize: 2, sortBy: 'centreName', sortDirection: 'Ascending' });

    expect(page.items.map((item) => item.centreCode)).toEqual(['C']);
    expect(page).toMatchObject({ totalCount: 3, totalPages: 2, hasPreviousPage: true, hasNextPage: false });
  });
});

describe('exported fixtures', () => {
  const query = toReportQuery(parse('country=AE&compare=0').filters);

  it('serve the exported fixture and echo the chosen filters back', async () => {
    const summary = await fixtureExecutiveSummaryRepository().get(query);

    expect(summary.meta.dataSource).toBe('Mock');
    expect(summary.meta.filter).toMatchObject({ countryCode: 'AE', compare: 'None' });
    expect(summary.kpis.map((kpi) => kpi.key)).toContain('applicationsReceived');
    expect(summary.topCountries.length).toBeGreaterThan(0);
  });

  it('serve every VAS section in the shape the screens read', async () => {
    const vas = fixtureVasRepository();
    const paging = { page: 1, pageSize: 10, sortBy: 'received', sortDirection: 'Descending' } as const;

    const [applications, outcomes, processing, revenue, visaTypes, centres] = await Promise.all([
      vas.getApplications(query),
      vas.getOutcomes(query),
      vas.getProcessing(query),
      vas.getRevenue(query),
      vas.getVisaTypes(query),
      vas.getCentres(query, paging),
    ]);

    expect(applications.byCountry).toHaveLength(6);
    expect(outcomes.kpis).toHaveLength(5);
    expect(processing.stageFunnel.map((stage) => stage.stage)).toEqual(['Submitted', 'DocumentCheck', 'Biometrics', 'Review', 'Decision']);
    expect(revenue.byService[0]?.currency).toBe('AED');
    expect(visaTypes.visaTypes).toHaveLength(8);
    expect(centres.centres).toMatchObject({ totalCount: 15, page: 1, totalPages: 2 });
    expect(centres.centres.items).toHaveLength(10);

    expect(isEmptyReport({ section: 'applications', data: applications })).toBe(false);
    expect(isEmptyReport({ section: 'centres', data: { ...centres, centres: { ...centres.centres, totalCount: 0 } } })).toBe(true);
  });
});

describe('small helpers', () => {
  it('knows which screens a permission list lets the user view', () => {
    const permissions = [{ screen: 'VAS', actions: ['View'] }];

    expect(canView(permissions, 'VAS')).toBe(true);
    expect(canView(permissions, 'FINANCE')).toBe(false);
  });

  it('accepts only the six VAS sections from the URL', () => {
    expect(isVasSection('centres')).toBe(true);
    expect(isVasSection('admin')).toBe(false);
    expect(isVasSection(null)).toBe(false);
  });
});
