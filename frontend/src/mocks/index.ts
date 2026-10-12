import type { ReportMeta, ReportQuery } from '@/shared/models/reporting';

// The fixtures are exported from the real backend (backend/mock-data/fixture-exporter), so they
// always have the API's shape. They are loaded on demand, so a live build never downloads them.
const fixtures = import.meta.glob<{ default: unknown }>('./fixtures/*.json');

/** How long a mock call takes, so loading states can be seen. Tests pass 0. */
export const MOCK_DELAY_MS = 350;

export async function loadFixture<T>(name: string, delayMs = MOCK_DELAY_MS): Promise<T> {
  const load = fixtures[`./fixtures/${name}.json`];
  if (!load) {
    throw new Error(`Unknown fixture: ${name}`);
  }

  const [module] = await Promise.all([load(), new Promise((resolve) => setTimeout(resolve, delayMs))]);
  return structuredClone(module.default) as T;
}

/**
 * Mock figures are one fixed export, so they do not change with the filters. The chosen filters
 * are still echoed back in the report's meta, as the API does, so the page reflects the selection.
 */
export function echoFilters<T extends { meta: ReportMeta }>(report: T, query: ReportQuery): T {
  const text = (value: unknown) => (typeof value === 'string' && value !== '' ? value : null);

  return {
    ...report,
    meta: {
      ...report.meta,
      filter: {
        ...report.meta.filter,
        countryCode: text(query.countryCode),
        centreCode: text(query.centreCode),
        visaTypeCode: text(query.visaTypeCode),
        compare: query.compare === 'None' ? 'None' : 'PreviousPeriod',
      },
    },
  };
}
