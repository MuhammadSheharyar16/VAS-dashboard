import type { Repositories } from '@/app/repositories';
import type { ExecutiveSummary, ExecutiveSummaryRepository } from '@/features/executive-summary/api/executiveSummaryRepository';
import type { LookupRepository } from '@/features/filters/api/lookupRepository';
import type { CurrentUser, MenuItem, Permission, SessionRepository } from '@/features/session/api/sessionRepository';
import type {
  VasApplications,
  VasCentres,
  VasOutcomes,
  VasProcessing,
  VasRepository,
  VasRevenue,
  VasVisaTypes,
} from '@/features/vas/api/vasRepository';
import type { LookupItem, ReportMeta, ReportQuery } from '@/shared/models/reporting';
import { pageCentres } from './centrePaging';

// The fixtures are exported from the real backend (backend/mock-data/fixture-exporter), so they
// always have the API's shape. They exist for the tests only; the app itself never reads them.
const fixtures = import.meta.glob<unknown>('../fixtures/*.json', { eager: true, import: 'default' });

export function loadFixture<T>(name: string): T {
  const fixture = fixtures[`../fixtures/${name}.json`];
  if (fixture === undefined) {
    throw new Error(`Unknown fixture: ${name}`);
  }

  return structuredClone(fixture) as T;
}

/** The fixtures are one fixed export; the chosen filters are echoed back in the meta, as the API does. */
function echoFilters<T extends { meta: ReportMeta }>(report: T, query: ReportQuery): T {
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

const report = <T extends { meta: ReportMeta }>(name: string, query: ReportQuery) =>
  Promise.resolve(echoFilters(loadFixture<T>(name), query));

export function fixtureSessionRepository(): SessionRepository {
  return {
    getCurrentUser: () => Promise.resolve(loadFixture<CurrentUser>('auth-me')),
    getMenu: () => Promise.resolve(loadFixture<MenuItem[]>('auth-menu')),
    getPermissions: () => Promise.resolve(loadFixture<Permission[]>('auth-permissions')),
  };
}

export function fixtureLookupRepository(): LookupRepository {
  return {
    getCountries: () => Promise.resolve(loadFixture<LookupItem[]>('lookups-countries')),
    getCentres: () => Promise.resolve(loadFixture<LookupItem[]>('lookups-centres')),
    getVisaTypes: () => Promise.resolve(loadFixture<LookupItem[]>('lookups-visa-types')),
  };
}

export function fixtureExecutiveSummaryRepository(): ExecutiveSummaryRepository {
  return { get: (query) => report<ExecutiveSummary>('executive-summary', query) };
}

export function fixtureVasRepository(): VasRepository {
  return {
    getApplications: (query) => report<VasApplications>('vas-applications', query),
    getOutcomes: (query) => report<VasOutcomes>('vas-outcomes', query),
    getProcessing: (query) => report<VasProcessing>('vas-processing', query),
    getRevenue: (query) => report<VasRevenue>('vas-revenue', query),
    getVisaTypes: (query) => report<VasVisaTypes>('vas-visa-types', query),
    getCentres: async (query, paging) => {
      const centres = await report<VasCentres>('vas-centres', query);
      return { ...centres, centres: pageCentres(centres.centres.items, paging) };
    },
  };
}

/** Fakes at the data boundary, serving the backend's exported output. */
export function fixtureRepositories(): Repositories {
  return {
    session: fixtureSessionRepository(),
    lookups: fixtureLookupRepository(),
    executiveSummary: fixtureExecutiveSummaryRepository(),
    vas: fixtureVasRepository(),
  };
}
