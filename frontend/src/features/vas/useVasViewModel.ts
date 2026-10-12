import { useCallback, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useRepositories } from '@/app/repositories';
import { useFilterLookups } from '@/features/filters/useFilterLookups';
import type { FilterLookups } from '@/shared/components/FilterBar';
import { useReportFilters, type ReportFilterState } from '@/shared/hooks/useReportFilters';
import { useReportQuery, type ReportState } from '@/shared/hooks/useReportQuery';
import { toReportQuery, type ReportQuery, type SortDirection } from '@/shared/models/reporting';
import {
  CENTRE_SORT_FIELDS,
  type CentrePaging,
  type CentreSortField,
  type VasApplications,
  type VasCentres,
  type VasOutcomes,
  type VasProcessing,
  type VasRepository,
  type VasRevenue,
  type VasVisaTypes,
} from './api/vasRepository';

export const VAS_SECTIONS = ['applications', 'outcomes', 'processing', 'revenue', 'visa-types', 'centres'] as const;
export type VasSection = (typeof VAS_SECTIONS)[number];

export const CENTRE_PAGE_SIZE = 10;

/** The report of the section on screen, tagged with its section so the screen can tell which it has. */
export type VasReport =
  | { section: 'applications'; data: VasApplications }
  | { section: 'outcomes'; data: VasOutcomes }
  | { section: 'processing'; data: VasProcessing }
  | { section: 'revenue'; data: VasRevenue }
  | { section: 'visa-types'; data: VasVisaTypes }
  | { section: 'centres'; data: VasCentres };

export interface CentreTableState extends CentrePaging {
  setPage(page: number): void;
  setSort(field: string, direction: SortDirection): void;
}

export interface VasViewModel {
  filterState: ReportFilterState;
  lookups: FilterLookups;
  section: VasSection;
  setSection(section: VasSection): void;
  report: ReportState<VasReport>;
  centreTable: CentreTableState;
}

export function isVasSection(value: string | null): value is VasSection {
  return VAS_SECTIONS.includes(value as VasSection);
}

async function fetchSection(
  vas: VasRepository,
  section: VasSection,
  query: ReportQuery,
  paging: CentrePaging,
  signal: AbortSignal,
): Promise<VasReport> {
  switch (section) {
    case 'applications':
      return { section, data: await vas.getApplications(query, signal) };
    case 'outcomes':
      return { section, data: await vas.getOutcomes(query, signal) };
    case 'processing':
      return { section, data: await vas.getProcessing(query, signal) };
    case 'revenue':
      return { section, data: await vas.getRevenue(query, signal) };
    case 'visa-types':
      return { section, data: await vas.getVisaTypes(query, signal) };
    case 'centres':
      return { section, data: await vas.getCentres(query, paging, signal) };
  }
}

/** True when a section's report has nothing to show for the selection. */
export function isEmptyReport(report: VasReport): boolean {
  switch (report.section) {
    case 'applications':
      return report.data.byCountry.length === 0;
    case 'outcomes':
      return report.data.outcomes.every((outcome) => outcome.count === 0);
    case 'processing':
      return report.data.statuses.length === 0;
    case 'revenue':
      return report.data.byService.length === 0;
    case 'visa-types':
      return report.data.visaTypes.every((visaType) => visaType.received === 0);
    case 'centres':
      return report.data.centres.totalCount === 0;
  }
}

/**
 * Owns the VAS screen's state: the filters, which of the six sections is open, and that section's
 * report. Only the open section is requested.
 */
export function useVasViewModel(): VasViewModel {
  const { vas } = useRepositories();
  const filterState = useReportFilters();
  const lookups = useFilterLookups();
  const [params, setParams] = useSearchParams();

  const sectionParam = params.get('section');
  const section: VasSection = isVasSection(sectionParam) ? sectionParam : 'applications';

  const setSection = useCallback(
    (next: VasSection) => {
      setParams(
        (current) => {
          const updated = new URLSearchParams(current);
          if (next === 'applications') {
            updated.delete('section');
          } else {
            updated.set('section', next);
          }

          return updated;
        },
        { replace: true },
      );
    },
    [setParams],
  );

  // The centre table goes back to page 1 whenever the filters change.
  const filterSignature = JSON.stringify(toReportQuery(filterState.filters));
  const [table, setTable] = useState({
    page: 1,
    sortBy: 'centreName' as CentreSortField,
    sortDirection: 'Ascending' as SortDirection,
    signature: filterSignature,
  });

  const paging: CentrePaging = {
    page: table.signature === filterSignature ? table.page : 1,
    pageSize: CENTRE_PAGE_SIZE,
    sortBy: table.sortBy,
    sortDirection: table.sortDirection,
  };

  const centreTable: CentreTableState = {
    ...paging,
    setPage: (page) => setTable((current) => ({ ...current, page: Math.max(1, page), signature: filterSignature })),
    setSort: (field, direction) => {
      // Only a field on the list is ever sent to the API.
      const sortBy = CENTRE_SORT_FIELDS.find((allowed) => allowed === field);
      if (sortBy) {
        setTable({ page: 1, sortBy, sortDirection: direction, signature: filterSignature });
      }
    },
  };

  const report = useReportQuery<VasReport>({
    key: ['vas', section],
    filterState,
    params: section === 'centres' ? paging : null,
    fetch: (query, signal) => fetchSection(vas, section, query, paging, signal),
    isEmpty: isEmptyReport,
  });

  return { filterState, lookups, section, setSection, report, centreTable };
}
