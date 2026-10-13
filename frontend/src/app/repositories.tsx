import { createContext, useContext, useMemo, type ReactNode } from 'react';
import { useAuth } from '@/core/auth/session';
import type { AppEnv } from '@/core/config/env';
import { createApiClient } from '@/core/http/apiClient';
import {
  createLiveExecutiveSummaryRepository,
  type ExecutiveSummaryRepository,
} from '@/features/executive-summary/api/executiveSummaryRepository';
import { createLiveLookupRepository, type LookupRepository } from '@/features/filters/api/lookupRepository';
import { createLiveSessionRepository, type SessionRepository } from '@/features/session/api/sessionRepository';
import { createLiveVasRepository, type VasRepository } from '@/features/vas/api/vasRepository';

export interface Repositories {
  session: SessionRepository;
  lookups: LookupRepository;
  executiveSummary: ExecutiveSummaryRepository;
  vas: VasRepository;
}

const RepositoryContext = createContext<Repositories | null>(null);

/**
 * Builds the repositories over the API. Screens and view-models ask for a repository and never
 * call the API themselves. Whether the data is mock or live is the API's decision, not the app's.
 */
export function RepositoryProvider({ env, children }: { env: AppEnv; children: ReactNode }) {
  const { tokens } = useAuth();

  const repositories = useMemo<Repositories>(() => {
    const api = createApiClient(env.apiBaseUrl, tokens);
    return {
      session: createLiveSessionRepository(api),
      lookups: createLiveLookupRepository(api),
      executiveSummary: createLiveExecutiveSummaryRepository(api),
      vas: createLiveVasRepository(api),
    };
  }, [env, tokens]);

  return <RepositoryContext.Provider value={repositories}>{children}</RepositoryContext.Provider>;
}

/** For tests: supply fakes at the data boundary. */
export function TestRepositoryProvider({ value, children }: { value: Repositories; children: ReactNode }) {
  return <RepositoryContext.Provider value={value}>{children}</RepositoryContext.Provider>;
}

export function useRepositories(): Repositories {
  const repositories = useContext(RepositoryContext);
  if (!repositories) {
    throw new Error('useRepositories must be used inside RepositoryProvider');
  }

  return repositories;
}
