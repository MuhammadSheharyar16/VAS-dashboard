import { BarChart3, LayoutGrid, LogOut, Moon, Sun } from 'lucide-react';
import { AnimatePresence, motion } from 'motion/react';
import { useState } from 'react';
import { Link, useLocation, useOutlet } from 'react-router-dom';
import { useAuth } from '@/core/auth/session';
import { t } from '@/core/i18n';
import { useTheme } from '@/core/theme/ThemeProvider';
import { DubaiSkyline } from '@/shared/components/DubaiSkyline';
import { carriedSearch } from '@/shared/utils/carriedFilters';
import './shell.css';

const HOME_ROUTE = '/';

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? (parts[parts.length - 1]?.[0] ?? '') : '')).toUpperCase() || '?';
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

/** The bar every signed-in page shares: the way home, the theme switch and the signed-in user. */
function Topbar({ isHome }: { isHome: boolean }) {
  const { account, signOut } = useAuth();
  const location = useLocation();
  // The filters go home with the user, so the next report they open keeps them.
  const home = { pathname: HOME_ROUTE, search: carriedSearch(location.search) };

  return (
    <header className="topbar">
      <Link to={home} className="brand" aria-label={t('app.home')}>
        <span className="brand__mark" aria-hidden="true">
          <BarChart3 size={21} />
        </span>
        <span className="brand__text">
          <span className="brand__name">{t('app.name')}</span>
          <span className="brand__product">{t('app.product')}</span>
        </span>
      </Link>

      {!isHome && (
        <Link to={home} className="button topbar__home">
          <LayoutGrid size={16} aria-hidden="true" />
          {t('app.allAreas')}
        </Link>
      )}

      <div className="topbar__end">
        <ThemeToggle />
        <div className="account">
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
      </div>
    </header>
  );
}

/**
 * The page as it was when this copy mounted. While a page fades out, the router already points at
 * the next one; without this the leaving frame would show the next page for a moment, and that
 * page would then mount a second time and lose whatever the user had done in it.
 */
function PageAtMount() {
  const outlet = useOutlet();
  const [page] = useState(outlet);

  return page;
}

/** The signed-in frame: the top bar, the home backdrop and the current page. */
export function AppShell() {
  const location = useLocation();
  const isHome = location.pathname === HOME_ROUTE;

  return (
    <div className={`shell ${isHome ? 'shell--home' : ''}`}>
      <a className="skip-link" href="#main">
        {t('app.skipToContent')}
      </a>

      {/* The backdrop sits outside the page transition, so it does not move with the page. */}
      <AnimatePresence>
        {isHome && (
          <motion.div
            key="backdrop"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.6 }}
          >
            <DubaiSkyline />
          </motion.div>
        )}
      </AnimatePresence>

      <Topbar isHome={isHome} />

      <main id="main" className="content" tabIndex={-1}>
        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={location.pathname}
            className="content__inner"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.22, ease: 'easeOut' }}
          >
            <PageAtMount />
          </motion.div>
        </AnimatePresence>
      </main>
    </div>
  );
}
