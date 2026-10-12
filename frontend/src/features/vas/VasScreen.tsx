import { t } from '@/core/i18n';
import { FilterBar } from '@/shared/components/FilterBar';
import { PageHeader } from '@/shared/components/PageHeader';
import { ReportBody } from '@/shared/components/ReportBody';
import { Segmented } from '@/shared/components/Segmented';
import { ApplicationsSection } from './components/ApplicationsSection';
import { CentresSection } from './components/CentresSection';
import { OutcomesSection } from './components/OutcomesSection';
import { ProcessingSection } from './components/ProcessingSection';
import { RevenueSection } from './components/RevenueSection';
import { VisaTypesSection } from './components/VisaTypesSection';
import { useVasViewModel, VAS_SECTIONS, type CentreTableState, type VasReport } from './useVasViewModel';

const SECTION_REGION_ID = 'vas-section';

const SKELETON_CARDS: Record<VasReport['section'], number> = {
  applications: 3,
  outcomes: 5,
  processing: 4,
  revenue: 2,
  'visa-types': 0,
  centres: 0,
};

function Section({ report, centreTable }: { report: VasReport; centreTable: CentreTableState }) {
  switch (report.section) {
    case 'applications':
      return <ApplicationsSection report={report.data} />;
    case 'outcomes':
      return <OutcomesSection report={report.data} />;
    case 'processing':
      return <ProcessingSection report={report.data} />;
    case 'revenue':
      return <RevenueSection report={report.data} />;
    case 'visa-types':
      return <VisaTypesSection report={report.data} />;
    case 'centres':
      return <CentresSection report={report.data} table={centreTable} />;
  }
}

/** The VAS screen: six report sections behind one set of filters. It only lays out the view-model. */
export function VasScreen() {
  const { filterState, lookups, section, setSection, report, centreTable } = useVasViewModel();
  const meta = report.status === 'content' || report.status === 'empty' ? report.data.data.meta : undefined;

  return (
    <div className="screen">
      <PageHeader title={t('vas.title')} subtitle={t('vas.subtitle')} meta={meta} />
      <FilterBar state={filterState} lookups={lookups} />

      <div className="section-bar">
        <Segmented
          label={t('vas.sections')}
          controls={SECTION_REGION_ID}
          value={section}
          onChange={setSection}
          options={VAS_SECTIONS.map((value) => ({ value, label: t(`vas.section.${value}`) }))}
        />
      </div>

      <div id={SECTION_REGION_ID} role="tabpanel" aria-label={t(`vas.section.${section}`)}>
        <ReportBody state={report} skeletonCards={SKELETON_CARDS[section]}>
          {(loaded) => <Section key={loaded.section} report={loaded} centreTable={centreTable} />}
        </ReportBody>
      </div>
    </div>
  );
}
