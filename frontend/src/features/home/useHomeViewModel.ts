import type { LucideIcon } from 'lucide-react';
import { useMemo } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from '@/core/auth/session';
import { t, tOr } from '@/core/i18n';
import type { MenuItem } from '@/features/session/api/sessionRepository';
import { carriedSearch } from '@/shared/utils/carriedFilters';
import { areaIcon } from './areaIcons';

export interface HomeArea {
  code: string;
  title: string;
  description: string;
  icon: LucideIcon;
  /** Its place in the menu, as shown on the card: "01", "02", ... */
  position: string;
  isComingSoon: boolean;
  to: { pathname: string; search: string };
}

export interface HomeViewModel {
  /** The name to greet the user by. */
  greetingName: string;
  areas: HomeArea[];
}

/** Owns the home screen's state: who to greet and the reporting areas the backend's menu lists. */
export function useHomeViewModel(menu: MenuItem[]): HomeViewModel {
  const { account } = useAuth();
  const { search } = useLocation();

  const areas = useMemo(() => {
    const filters = carriedSearch(search);

    return menu.map((item, index) => ({
      code: item.code,
      title: tOr(`nav.${item.code}`, item.title),
      description: tOr(`home.area.${item.code}`, t('home.area.default')),
      icon: areaIcon(item.code),
      position: String(index + 1).padStart(2, '0'),
      isComingSoon: item.isComingSoon,
      // A planned area has no report to filter, so it gets a clean address.
      to: { pathname: item.route, search: item.isComingSoon ? '' : filters },
    }));
  }, [menu, search]);

  return { greetingName: account.name.trim().split(/\s+/)[0] || account.name, areas };
}
