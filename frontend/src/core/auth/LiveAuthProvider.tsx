import {
  InteractionRequiredAuthError,
  PublicClientApplication,
  type AccountInfo,
} from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { loadRuntimeConfig } from '@/core/config/runtimeConfig';
import { AuthContext, type AuthSession } from './session';

interface SignedIn {
  client: PublicClientApplication;
  account: AccountInfo;
  scopes: string[];
}

const RETURN_TO_KEY = 'vas.returnTo';

/** Remembers the address the user opened, so a shared or bookmarked view survives the sign-in round trip. */
function rememberReturnAddress(): void {
  try {
    sessionStorage.setItem(RETURN_TO_KEY, window.location.pathname + window.location.search);
  } catch {
    // Without storage the user lands on the home page after sign-in.
  }
}

/** Puts the remembered address back before the router reads it. Only a path on this site is accepted. */
function restoreReturnAddress(): void {
  try {
    const returnTo = sessionStorage.getItem(RETURN_TO_KEY);
    sessionStorage.removeItem(RETURN_TO_KEY);

    if (returnTo?.startsWith('/') && !returnTo.startsWith('//')) {
      window.history.replaceState(null, '', returnTo);
    }
  } catch {
    // Nothing remembered.
  }
}

/** Resolves to null when the browser is on its way to the sign-in page. */
async function startSignIn(): Promise<SignedIn | null> {
  const config = await loadRuntimeConfig();
  const client = new PublicClientApplication({
    auth: {
      clientId: config.clientId,
      authority: config.authority,
      redirectUri: window.location.origin,
      postLogoutRedirectUri: window.location.origin,
    },
    cache: { cacheLocation: 'sessionStorage' },
  });

  await client.initialize();
  const redirectResult = await client.handleRedirectPromise();
  const account = redirectResult?.account ?? client.getActiveAccount() ?? client.getAllAccounts()[0];
  const scopes = [config.apiScope];

  if (!account) {
    // Leaves the page; the fallback stays up until the browser navigates.
    rememberReturnAddress();
    await client.loginRedirect({ scopes });
    return null;
  }

  if (redirectResult) {
    restoreReturnAddress();
  }

  client.setActiveAccount(account);
  return { client, account, scopes };
}

let signInOnce: Promise<SignedIn | null> | null = null;

/** One sign-in per page load, however many times the provider mounts (React mounts it twice in development). */
function signIn(): Promise<SignedIn | null> {
  signInOnce ??= startSignIn();
  return signInOnce;
}

type State = { phase: 'starting' } | { phase: 'failed' } | ({ phase: 'ready' } & SignedIn);

interface LiveAuthProviderProps {
  fallback: ReactNode;
  errorFallback: ReactNode;
  children: ReactNode;
}

/**
 * Microsoft Entra ID sign-in: authorization code flow with PKCE, by redirect.
 * Tokens live in MSAL's sessionStorage cache. They are never logged, shown or put in a URL.
 */
export default function LiveAuthProvider({ fallback, errorFallback, children }: LiveAuthProviderProps) {
  const [state, setState] = useState<State>({ phase: 'starting' });

  useEffect(() => {
    let cancelled = false;

    signIn()
      .then((signedIn) => {
        if (signedIn && !cancelled) {
          setState({ phase: 'ready', ...signedIn });
        }
      })
      .catch(() => {
        if (!cancelled) {
          setState({ phase: 'failed' });
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const session = useMemo<AuthSession | null>(() => {
    if (state.phase !== 'ready') {
      return null;
    }

    const { client, account, scopes } = state;

    return {
      account: { name: account.name ?? account.username, username: account.username },
      tokens: {
        async getAccessToken(forceRefresh = false) {
          try {
            const result = await client.acquireTokenSilent({ scopes, account, forceRefresh });
            return result.accessToken;
          } catch (error) {
            if (error instanceof InteractionRequiredAuthError) {
              await client.acquireTokenRedirect({ scopes, account });
            }

            throw error;
          }
        },
      },
      signOut: () => {
        void client.logoutRedirect({ account });
      },
    };
  }, [state]);

  if (state.phase === 'failed') {
    return <>{errorFallback}</>;
  }

  if (state.phase !== 'ready' || !session) {
    return <>{fallback}</>;
  }

  return (
    <MsalProvider instance={state.client}>
      <AuthContext.Provider value={session}>{children}</AuthContext.Provider>
    </MsalProvider>
  );
}
