import { useQuery } from '@tanstack/react-query';
import { useRepositories } from '@/app/repositories';
import { ApiError } from '@/core/http/apiClient';
import type { CurrentUser, MenuItem, Permission } from './api/sessionRepository';

export type SessionState =
  | { status: 'loading' }
  | { status: 'forbidden' }
  | { status: 'error'; error: unknown; retry(): void }
  | { status: 'ready'; user: CurrentUser; menu: MenuItem[]; permissions: Permission[] };

/**
 * Who is signed in, what they may see and the menu to show them, all from the backend.
 * A 403 here means "signed in but not assigned the role", which is not the same as "sign in again".
 */
export function useSession(): SessionState {
  const { session } = useRepositories();

  const result = useQuery({
    queryKey: ['session'],
    queryFn: async () => {
      const [user, menu, permissions] = await Promise.all([
        session.getCurrentUser(),
        session.getMenu(),
        session.getPermissions(),
      ]);

      return { user, menu: [...menu].sort((a, b) => a.order - b.order), permissions };
    },
    staleTime: Infinity,
    retry: (failureCount, error) => !(error instanceof ApiError && error.kind === 'forbidden') && failureCount < 2,
  });

  if (result.isError) {
    return result.error instanceof ApiError && result.error.kind === 'forbidden'
      ? { status: 'forbidden' }
      : { status: 'error', error: result.error, retry: () => void result.refetch() };
  }

  return result.data ? { status: 'ready', ...result.data } : { status: 'loading' };
}
