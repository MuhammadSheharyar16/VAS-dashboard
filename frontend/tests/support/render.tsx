import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { TestRepositoryProvider, type Repositories } from '@/app/repositories';
import { AuthContext, type AuthSession } from '@/core/auth/session';
import { ThemeProvider } from '@/core/theme/ThemeProvider';
import { createMockExecutiveSummaryRepository } from '@/features/executive-summary/api/executiveSummaryRepository';
import { createMockLookupRepository } from '@/features/filters/api/lookupRepository';
import { createMockSessionRepository } from '@/features/session/api/sessionRepository';
import { createMockVasRepository } from '@/features/vas/api/vasRepository';

/** The real mock repositories with no artificial delay: the same fixtures the app shows in mock mode. */
export function fixtureRepositories(): Repositories {
  return {
    session: createMockSessionRepository(0),
    lookups: createMockLookupRepository(0),
    executiveSummary: createMockExecutiveSummaryRepository(0),
    vas: createMockVasRepository(0),
  };
}

export const testSession: AuthSession = {
  account: { name: 'Test Executive', username: 'executive@example.test' },
  tokens: { getAccessToken: () => Promise.resolve('test-token') },
  signOut: () => undefined,
};

interface RenderOptions {
  route?: string;
  repositories?: Partial<Repositories>;
  session?: AuthSession;
}

/** Renders inside every provider the app uses, with fakes at the data boundary. */
export function renderApp(ui: ReactElement, { route = '/', repositories = {}, session = testSession }: RenderOptions = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <AuthContext.Provider value={session}>
          <TestRepositoryProvider value={{ ...fixtureRepositories(), ...repositories }}>
            <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
          </TestRepositoryProvider>
        </AuthContext.Provider>
      </QueryClientProvider>
    </ThemeProvider>,
  );
}
