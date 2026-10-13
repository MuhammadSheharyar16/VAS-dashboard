import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MotionConfig } from 'motion/react';
import { useState } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '@/core/auth/AuthProvider';
import { ConfigError, readEnv, type AppEnv } from '@/core/config/env';
import { ApiError } from '@/core/http/apiClient';
import { ThemeProvider } from '@/core/theme/ThemeProvider';
import { ConfigErrorPage, SignInFailedPage, StartingPage } from './pages';
import { RepositoryProvider } from './repositories';
import { AppRouter } from './router';

function Aurora() {
  return (
    <div className="aurora" aria-hidden="true">
      <span className="aurora__light aurora__light--1" />
      <span className="aurora__light aurora__light--2" />
      <span className="aurora__light aurora__light--3" />
      <span className="aurora__grid" />
    </div>
  );
}

function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 60_000,
        refetchOnWindowFocus: false,
        // A request the server refused will be refused again; only transient failures are retried.
        retry: (failureCount, error) =>
          failureCount < 2 && !(error instanceof ApiError && ['forbidden', 'validation', 'notFound'].includes(error.kind)),
      },
    },
  });
}

function readEnvOrSetting(): AppEnv | string {
  try {
    return readEnv();
  } catch (error) {
    return error instanceof ConfigError ? error.message : 'configuration';
  }
}

export function App() {
  const [queryClient] = useState(createQueryClient);
  const [env] = useState(readEnvOrSetting);

  return (
    <ThemeProvider>
      {/* Motion's own animations stop for anyone who asked for reduced motion. */}
      <MotionConfig reducedMotion="user">
        <Aurora />
        {typeof env === 'string' ? (
          <ConfigErrorPage setting={env} />
        ) : (
          <QueryClientProvider client={queryClient}>
            <AuthProvider fallback={<StartingPage />} errorFallback={<SignInFailedPage />}>
              <RepositoryProvider env={env}>
                <BrowserRouter>
                  <AppRouter />
                </BrowserRouter>
              </RepositoryProvider>
            </AuthProvider>
          </QueryClientProvider>
        )}
      </MotionConfig>
    </ThemeProvider>
  );
}
