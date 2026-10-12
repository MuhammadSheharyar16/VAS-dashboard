import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { AppRouter } from '@/app/router';
import { ApiError } from '@/core/http/apiClient';
import { t } from '@/core/i18n';
import { ExecutiveSummaryScreen } from '@/features/executive-summary/ExecutiveSummaryScreen';
import {
  createMockExecutiveSummaryRepository,
  type ExecutiveSummary,
  type ExecutiveSummaryRepository,
} from '@/features/executive-summary/api/executiveSummaryRepository';
import { createMockSessionRepository } from '@/features/session/api/sessionRepository';
import { VasScreen } from '@/features/vas/VasScreen';
import { KpiCard } from '@/shared/components/KpiCard';
import { StatusBadge } from '@/shared/components/StatusBadge';
import type { KpiCard as KpiCardModel } from '@/shared/models/reporting';
import { renderApp } from '../support/render';

const kpi = (overrides: Partial<KpiCardModel> = {}): KpiCardModel => ({
  key: 'approvalRate',
  value: 83.2,
  unit: 'Percent',
  currency: null,
  previousValue: 81.3,
  changePercent: 2.3,
  trend: 'Up',
  status: 'Good',
  ...overrides,
});

describe('KpiCard', () => {
  it('shows the label, the value, the status and the change in words', () => {
    renderApp(<KpiCard kpi={kpi()} />);

    const card = screen.getByRole('article', { name: t('kpi.approvalRate') });
    expect(within(card).getByText('83.2%')).toBeInTheDocument();
    expect(within(card).getByText(t('status.Good'))).toBeInTheDocument();
    expect(within(card).getAllByText(t('kpi.changeUp', { value: '2.3%' })).length).toBeGreaterThan(0);
    expect(within(card).getByText(t('kpi.previous', { value: '81.3%' }))).toBeInTheDocument();
  });

  it('shows the currency beside a revenue value and no status badge when there is no threshold', () => {
    renderApp(<KpiCard kpi={kpi({ key: 'vasRevenue', unit: 'Currency', currency: 'AED', value: 571700, status: 'Neutral' })} />);

    expect(screen.getByText('571,700')).toBeInTheDocument();
    expect(screen.getByText('AED')).toBeInTheDocument();
    expect(screen.queryByText(t('status.Neutral'))).not.toBeInTheDocument();
  });

  it('says there is no comparison when the previous period was not requested', () => {
    renderApp(<KpiCard kpi={kpi({ previousValue: null, changePercent: null, trend: 'Flat' })} />);

    expect(screen.getByText(t('kpi.noComparison'))).toBeInTheDocument();
  });

  it('shows a dash for a value the API could not give', () => {
    renderApp(<KpiCard kpi={kpi({ value: null, status: 'Neutral' })} />);

    expect(screen.getByRole('article').querySelector('.kpi__value')).toHaveTextContent('–');
  });
});

describe('StatusBadge', () => {
  it.each(['Good', 'Warning', 'Critical', 'Neutral'] as const)('gives %s a label, not only a colour', (status) => {
    renderApp(<StatusBadge status={status} />);

    expect(screen.getByText(t(`status.${status}`))).toBeInTheDocument();
  });
});

describe('ExecutiveSummaryScreen', () => {
  const repository = (get: ExecutiveSummaryRepository['get']): { executiveSummary: ExecutiveSummaryRepository } => ({
    executiveSummary: { get },
  });

  it('shows a loading state while the report is on its way', () => {
    renderApp(<ExecutiveSummaryScreen />, { repositories: repository(() => new Promise(() => undefined)) });

    expect(screen.getByRole('heading', { level: 1, name: t('executiveSummary.title') })).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent(t('state.loading'));
  });

  it('shows the KPIs, the charts, the data source and the freshness once loaded', async () => {
    renderApp(<ExecutiveSummaryScreen />);

    expect(await screen.findByRole('article', { name: t('kpi.applicationsReceived') })).toBeInTheDocument();
    expect(screen.getAllByRole('article')).toHaveLength(6);
    expect(screen.getByRole('heading', { name: t('executiveSummary.trendTitle') })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: t('executiveSummary.outcomesTitle') })).toBeInTheDocument();
    const countries = screen.getByRole('list', { name: t('executiveSummary.countriesTitle') });
    expect(within(countries).getByText('United Arab Emirates')).toBeInTheDocument();
    expect(screen.getByText(t('topbar.mockBadge'))).toBeInTheDocument();
    expect(screen.getByText(/Data as of 12 Oct 2026/)).toBeInTheDocument();
  });

  it('offers the trend as a table', async () => {
    const user = userEvent.setup();
    renderApp(<ExecutiveSummaryScreen />);

    const trend = (await screen.findByRole('heading', { name: t('executiveSummary.trendTitle') })).closest('section')!;
    await user.click(within(trend).getByRole('button', { name: t('chart.showTable') }));

    expect(within(trend).getByRole('table', { name: t('executiveSummary.trendTitle') })).toBeInTheDocument();
    expect(within(trend).getAllByRole('row').length).toBeGreaterThan(30);
  });

  it('shows the empty state when nothing was received in the period', async () => {
    const full = await createMockExecutiveSummaryRepository(0).get({});
    const empty: ExecutiveSummary = {
      ...full,
      kpis: full.kpis.map((card) => (card.key === 'applicationsReceived' ? { ...card, value: 0 } : card)),
    };

    renderApp(<ExecutiveSummaryScreen />, { repositories: repository(() => Promise.resolve(empty)) });

    expect(await screen.findByText(t('state.emptyTitle'))).toBeInTheDocument();
    expect(screen.queryByRole('article')).not.toBeInTheDocument();
  });

  it('shows the error state with a reference and loads again on retry', async () => {
    const user = userEvent.setup();
    const full = await createMockExecutiveSummaryRepository(0).get({});
    const get = vi
      .fn<ExecutiveSummaryRepository['get']>()
      .mockRejectedValueOnce(new ApiError('server', 500, [], 'trace-123'))
      .mockResolvedValue(full);

    renderApp(<ExecutiveSummaryScreen />, { repositories: repository(get) });

    expect(await screen.findByRole('alert')).toHaveTextContent(t('state.errorTitle'));
    expect(screen.getByText(t('state.traceId', { traceId: 'trace-123' }))).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: t('state.retry') }));

    expect(await screen.findByRole('article', { name: t('kpi.applicationsReceived') })).toBeInTheDocument();
  });

  it('asks for a new report with the chosen country and never with an unsafe one', async () => {
    const user = userEvent.setup();
    const full = await createMockExecutiveSummaryRepository(0).get({});
    const get = vi.fn<ExecutiveSummaryRepository['get']>().mockResolvedValue(full);

    renderApp(<ExecutiveSummaryScreen />, {
      route: `/?country=${encodeURIComponent("AE' OR 1=1")}`,
      repositories: repository(get),
    });

    await screen.findByRole('article', { name: t('kpi.applicationsReceived') });
    expect(get.mock.calls[0]?.[0]).toMatchObject({ countryCode: null, lastDays: 30 });

    // The choices arrive from the lookups a moment after the page.
    await screen.findByRole('option', { name: 'India' });
    await user.selectOptions(screen.getByLabelText(t('filters.country')), 'IN');

    await waitFor(() => expect(get.mock.calls.at(-1)?.[0]).toMatchObject({ countryCode: 'IN' }));
  });

  it('does not request a report while a custom range is not usable, and says why', async () => {
    const get = vi.fn<ExecutiveSummaryRepository['get']>();

    renderApp(<ExecutiveSummaryScreen />, {
      route: '/?period=custom&from=2024-01-01&to=2026-06-01',
      repositories: repository(get),
    });

    expect(await screen.findByRole('alert')).toHaveTextContent(t('filters.rangeTooLong'));
    expect(get).not.toHaveBeenCalled();
  });
});

describe('VasScreen', () => {
  it('opens on Applications and switches section from the tabs', async () => {
    const user = userEvent.setup();
    renderApp(<VasScreen />);

    expect(await screen.findByRole('heading', { name: t('vas.applications.byCountry') })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: t('vas.section.applications') })).toHaveAttribute('aria-selected', 'true');

    await user.click(screen.getByRole('tab', { name: t('vas.section.centres') }));

    expect(await screen.findByRole('table', { name: t('vas.centres.tableTitle') })).toBeInTheDocument();
    expect(screen.getByText(t('table.page', { page: 1, pages: 2 }))).toBeInTheDocument();
  });

  it('opens the section named in the URL and pages the centre table', async () => {
    const user = userEvent.setup();
    renderApp(<VasScreen />, { route: '/?section=centres' });

    const table = await screen.findByRole('table', { name: t('vas.centres.tableTitle') });
    expect(within(table).getAllByRole('row')).toHaveLength(11);

    await user.click(screen.getByRole('button', { name: t('table.next') }));

    expect(await screen.findByText(t('table.page', { page: 2, pages: 2 }))).toBeInTheDocument();
    await waitFor(() =>
      expect(within(screen.getByRole('table', { name: t('vas.centres.tableTitle') })).getAllByRole('row')).toHaveLength(6),
    );
  });
});

describe('AppRouter', () => {
  it('builds the menu from the backend and marks the areas that are coming soon', async () => {
    renderApp(<AppRouter />, { route: '/executive-summary' });

    const nav = await screen.findByRole('navigation', { name: t('nav.label') });
    expect(within(nav).getAllByRole('link').map((link) => link.textContent)).toEqual([
      'Executive Summary',
      'VAS',
      `Operations${t('nav.comingSoon')}`,
      `Finance${t('nav.comingSoon')}`,
      `Alfit${t('nav.comingSoon')}`,
      `CSAT${t('nav.comingSoon')}`,
    ]);
    expect(screen.getByText('Test Executive')).toBeInTheDocument();
  });

  it('shows the coming-soon page for a planned area and not-found for an unknown path', async () => {
    const { unmount } = renderApp(<AppRouter />, { route: '/finance' });
    expect(await screen.findByRole('heading', { name: t('comingSoon.title', { area: 'Finance' }) })).toBeInTheDocument();
    unmount();

    renderApp(<AppRouter />, { route: '/nowhere' });
    expect(await screen.findByRole('heading', { name: t('notFound.title') })).toBeInTheDocument();
  });

  it('shows the access-denied page, not a sign-in prompt, when the API answers 403', async () => {
    const forbidden = () => Promise.reject(new ApiError('forbidden', 403));

    renderApp(<AppRouter />, {
      repositories: { session: { getCurrentUser: forbidden, getMenu: forbidden, getPermissions: forbidden } },
    });

    expect(await screen.findByRole('heading', { name: t('unauthorized.title') })).toBeInTheDocument();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
  });

  it('sends a user without the screen permission to the access-denied page', async () => {
    const session = createMockSessionRepository(0);

    renderApp(<AppRouter />, {
      route: '/vas',
      repositories: { session: { ...session, getPermissions: () => Promise.resolve([{ screen: 'EXECUTIVE_SUMMARY', actions: ['View'] }]) } },
    });

    expect(await screen.findByRole('heading', { name: t('unauthorized.title') })).toBeInTheDocument();
  });

  it('switches between light and dark from the toggle', async () => {
    const user = userEvent.setup();
    renderApp(<AppRouter />, { route: '/finance' });

    await user.click(await screen.findByRole('button', { name: t('topbar.themeToDark') }));

    expect(document.documentElement.dataset.theme).toBe('dark');
    expect(screen.getByRole('button', { name: t('topbar.themeToLight') })).toBeInTheDocument();
  });
});
