import { useQuery } from '@tanstack/react-query';
import { useRepositories } from '@/app/repositories';
import type { FilterLookups } from '@/shared/components/FilterBar';

const EMPTY: FilterLookups = { countries: [], centres: [], visaTypes: [] };
const TEN_MINUTES = 10 * 60 * 1000;

/** The choices for the filter dropdowns. They change rarely, so they are fetched once and kept. */
export function useFilterLookups(): FilterLookups {
  const { lookups } = useRepositories();

  const { data } = useQuery({
    queryKey: ['lookups'],
    queryFn: async (): Promise<FilterLookups> => {
      const [countries, centres, visaTypes] = await Promise.all([
        lookups.getCountries(),
        lookups.getCentres(),
        lookups.getVisaTypes(),
      ]);

      return { countries, centres, visaTypes };
    },
    staleTime: TEN_MINUTES,
  });

  return data ?? EMPTY;
}
