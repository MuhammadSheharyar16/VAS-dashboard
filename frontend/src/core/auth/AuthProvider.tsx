import { lazy, Suspense, type ReactNode } from 'react';

// MSAL is downloaded on its own, after the first paint.
const LiveAuthProvider = lazy(() => import('./LiveAuthProvider'));

interface AuthProviderProps {
  /** Shown while sign-in is being set up or the browser is being redirected. */
  fallback: ReactNode;
  /** Shown when sign-in cannot be set up at all. */
  errorFallback: ReactNode;
  children: ReactNode;
}

/** Every run of the app signs in with Microsoft Entra ID. There is no signed-in user without it. */
export function AuthProvider({ fallback, errorFallback, children }: AuthProviderProps) {
  return (
    <Suspense fallback={fallback}>
      <LiveAuthProvider fallback={fallback} errorFallback={errorFallback}>
        {children}
      </LiveAuthProvider>
    </Suspense>
  );
}
