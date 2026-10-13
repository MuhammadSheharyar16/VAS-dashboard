import type { ApiClient } from '@/core/http/apiClient';

export interface CurrentUser {
  id: string;
  displayName: string | null;
  username: string | null;
  roles: string[];
}

export interface MenuItem {
  code: string;
  title: string;
  route: string;
  order: number;
  isComingSoon: boolean;
}

export interface Permission {
  screen: string;
  actions: string[];
}

export interface SessionRepository {
  getCurrentUser(): Promise<CurrentUser>;
  getMenu(): Promise<MenuItem[]>;
  getPermissions(): Promise<Permission[]>;
}

export function createLiveSessionRepository(api: ApiClient): SessionRepository {
  return {
    getCurrentUser: () => api.get<CurrentUser>('me'),
    getMenu: () => api.get<MenuItem[]>('menu'),
    getPermissions: () => api.get<Permission[]>('permissions'),
  };
}

/** True when the permission list grants viewing the screen. A convenience for the UI; the API enforces. */
export function canView(permissions: Permission[], screen: string): boolean {
  return permissions.some((permission) => permission.screen === screen && permission.actions.includes('View'));
}
