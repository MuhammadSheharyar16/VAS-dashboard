import { createContext, useContext } from 'react';
import type { TokenSource } from '@/core/http/apiClient';

export interface AuthAccount {
  name: string;
  username: string;
}

/** What the rest of the app knows about sign-in. It never sees a token directly. */
export interface AuthSession {
  account: AuthAccount;
  tokens: TokenSource;
  signOut(): void;
}

export const AuthContext = createContext<AuthSession | null>(null);

export function useAuth(): AuthSession {
  const session = useContext(AuthContext);
  if (!session) {
    throw new Error('useAuth must be used inside AuthProvider');
  }

  return session;
}
