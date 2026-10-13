import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { tOr } from '@/core/i18n';
import { canView, type MenuItem, type Permission } from '@/features/session/api/sessionRepository';
import { useSession } from '@/features/session/useSession';
import { ErrorState, ReportSkeleton } from '@/shared/components/states';
import { AppShell } from './AppShell';
import HomePage from './home/page';
import { ComingSoonPage, NotFoundPage, StartingPage, UnauthorizedPage } from './pages';

// Each report is its own download, fetched when its page is first opened.
const ExecutiveSummaryPage = lazy(() => import('./executive-summary/page'));
const VasPage = lazy(() => import('./vas/page'));

/** The screen code each report route needs. The backend enforces the same codes. */
const SCREEN = { executiveSummary: 'EXECUTIVE_SUMMARY', vas: 'VAS' } as const;

function Guarded({ screen, permissions, children }: { screen: string; permissions: Permission[]; children: React.ReactNode }) {
  return canView(permissions, screen) ? <>{children}</> : <Navigate to="/unauthorized" replace />;
}

/** A path that is not a report: a "coming soon" area if the menu lists it, otherwise not found. */
function OtherPage({ menu }: { menu: MenuItem[] }) {
  const { pathname } = useLocation();
  const area = menu.find((item) => item.isComingSoon && item.route === pathname);

  return area ? <ComingSoonPage area={tOr(`nav.${area.code}`, area.title)} /> : <NotFoundPage />;
}

export function AppRouter() {
  const session = useSession();

  if (session.status === 'loading') {
    return <StartingPage />;
  }

  if (session.status === 'forbidden') {
    return <UnauthorizedPage />;
  }

  if (session.status === 'error') {
    return (
      <main className="fullpage">
        <ErrorState error={session.error} onRetry={session.retry} />
      </main>
    );
  }

  const { menu, permissions } = session;

  return (
    <Routes>
      <Route path="/unauthorized" element={<UnauthorizedPage />} />
      <Route element={<AppShell />}>
        <Route index element={<HomePage menu={menu} />} />
        <Route
          path="/executive-summary"
          element={
            <Guarded screen={SCREEN.executiveSummary} permissions={permissions}>
              <Suspense fallback={<ReportSkeleton />}>
                <ExecutiveSummaryPage />
              </Suspense>
            </Guarded>
          }
        />
        <Route
          path="/vas"
          element={
            <Guarded screen={SCREEN.vas} permissions={permissions}>
              <Suspense fallback={<ReportSkeleton />}>
                <VasPage />
              </Suspense>
            </Guarded>
          }
        />
        <Route path="*" element={<OtherPage menu={menu} />} />
      </Route>
    </Routes>
  );
}
