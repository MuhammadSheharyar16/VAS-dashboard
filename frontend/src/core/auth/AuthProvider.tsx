import { lazy, Suspense, useMemo, type ReactNode } from 'react';
import type { AppEnv } from '@/core/config/env';
import { AuthContext, type AuthSession } from './session';

// MSAL is only downloaded when the app really signs in.
const LiveAuthProvider = lazy(() => import('./LiveAuthProvider'));

/** The fake signed-in user for mock mode. The API itself never has a bypass. */
export const MOCK_ACCOUNT = { name: 'Sample Executive', username: 'sample.executive@example.test' } as const;

function MockAuthProvider({ children }: { children: ReactNode }) {
  const session = useMemo<AuthSession>(
    () => ({
      account: MOCK_ACCOUNT,
      tokens: { getAccessToken: () => Promise.resolve('') },
      signOut: () => undefined,
    }),
    [],
  );

  return <AuthContext.Provider value={session}>{children}</AuthContext.Provider>;
}

interface AuthProviderProps {
  env: AppEnv;
  /** Shown while sign-in is being set up or the browser is being redirected. */
  fallback: ReactNode;
  /** Shown when sign-in cannot be set up at all. */
  errorFallback: ReactNode;
  children: ReactNode;
}

export function AuthProvider({ env, fallback, errorFallback, children }: AuthProviderProps) {
  if (env.useMock) {
    return <MockAuthProvider>{children}</MockAuthProvider>;
  }

  return (
    <Suspense fallback={fallback}>
      <LiveAuthProvider fallback={fallback} errorFallback={errorFallback}>
        {children}
      </LiveAuthProvider>
    </Suspense>
  );
}
