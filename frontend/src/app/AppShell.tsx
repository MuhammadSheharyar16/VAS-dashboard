import {
  BarChart3,
  Globe,
  HeartPulse,
  Landmark,
  LayoutDashboard,
  LogOut,
  Moon,
  Settings2,
  SmilePlus,
  Sun,
  type LucideIcon,
} from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/core/auth/session';
import { t, tOr } from '@/core/i18n';
import { useTheme } from '@/core/theme/ThemeProvider';
import type { MenuItem } from '@/features/session/api/sessionRepository';
import './shell.css';

const NAV_ICON: Record<string, LucideIcon> = {
  EXECUTIVE_SUMMARY: LayoutDashboard,
  VAS: Globe,
  OPERATIONS: Settings2,
  FINANCE: Landmark,
  ALFIT: HeartPulse,
  CSAT: SmilePlus,
};

/** The filters stay with the user as they move between the two reports. */
const CARRIED_PARAMS = ['period', 'from', 'to', 'country', 'centre', 'visaType', 'compare'];

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : '')).toUpperCase() || '?';
}

function Sidebar({ menu }: { menu: MenuItem[] }) {
  const { account, signOut } = useAuth();
  const location = useLocation();

  const carried = new URLSearchParams();
  new URLSearchParams(location.search).forEach((value, key) => {
    if (CARRIED_PARAMS.includes(key)) {
      carried.set(key, value);
    }
  });
  const search = carried.toString();

  return (
    <aside className="sidebar">
      <div className="brand">
        <span className="brand__mark" aria-hidden="true">
          <BarChart3 size={21} />
        </span>
        <span className="brand__text">
          <span className="brand__name">{t('app.name')}</span>
          <br />
          <span className="brand__product">{t('app.product')}</span>
        </span>
      </div>

      <nav aria-label={t('nav.label')}>
        <ul className="nav">
          {menu.map((item) => {
            const Icon = NAV_ICON[item.code] ?? LayoutDashboard;
            const isActive = location.pathname === item.route;

            return (
              <li key={item.code}>
                <NavLink
                  to={{ pathname: item.route, search: item.isComingSoon ? '' : search }}
                  className="nav__link"
                  title={tOr(`nav.${item.code}`, item.title)}
                >
                  {isActive && (
                    <motion.span
                      layoutId="nav-active"
                      className="nav__active"
                      transition={{ type: 'spring', stiffness: 360, damping: 32 }}
                    />
                  )}
                  <Icon size={19} className="nav__icon" aria-hidden="true" />
                  <span className="nav__text">{tOr(`nav.${item.code}`, item.title)}</span>
                  {item.isComingSoon && <span className="nav__tag">{t('nav.comingSoon')}</span>}
                </NavLink>
              </li>
            );
          })}
        </ul>
      </nav>

      <div className="sidebar__footer">
        <span className="avatar" aria-hidden="true">
          {initialsOf(account.name)}
        </span>
        <div className="user">
          <div className="user__name" title={t('topbar.signedInAs', { name: account.name })}>
            {account.name}
          </div>
          <div className="user__detail">{account.username}</div>
        </div>
        <button type="button" className="button button--icon button--ghost" onClick={signOut} aria-label={t('topbar.signOut')}>
          <LogOut size={17} aria-hidden="true" />
        </button>
      </div>
    </aside>
  );
}

function ThemeToggle() {
  const { mode, toggle } = useTheme();
  const Icon = mode === 'dark' ? Sun : Moon;

  return (
    <button
      type="button"
      className="button button--icon"
      onClick={toggle}
      aria-label={t(mode === 'dark' ? 'topbar.themeToLight' : 'topbar.themeToDark')}
    >
      <Icon size={17} aria-hidden="true" />
    </button>
  );
}

/** The signed-in frame: the menu the backend returned, the theme switch and the current page. */
export function AppShell({ menu }: { menu: MenuItem[] }) {
  const location = useLocation();

  return (
    <div className="shell">
      <a className="skip-link" href="#main">
        {t('app.skipToContent')}
      </a>

      <Sidebar menu={menu} />

      <div className="main">
        <div className="topbar">
          <ThemeToggle />
        </div>

        <main id="main" className="content" tabIndex={-1}>
          <AnimatePresence mode="wait" initial={false}>
            <motion.div
              key={location.pathname}
              className="content__inner"
              initial={{ opacity: 0, y: 12 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -8 }}
              transition={{ duration: 0.28, ease: [0.22, 1, 0.36, 1] }}
            >
              <Outlet />
            </motion.div>
          </AnimatePresence>
        </main>
      </div>
    </div>
  );
}
