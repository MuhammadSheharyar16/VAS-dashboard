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

    const start = async () => {
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
        await client.loginRedirect({ scopes });
        return;
      }

      client.setActiveAccount(account);
      if (!cancelled) {
        setState({ phase: 'ready', client, account, scopes });
      }
    };

    start().catch(() => {
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
