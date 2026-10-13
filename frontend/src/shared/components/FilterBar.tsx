import {
  Building2,
  CalendarDays,
  CalendarRange,
  ChevronDown,
  GitCompareArrows,
  Globe2,
  RotateCcw,
  SlidersHorizontal,
  Stamp,
  type LucideIcon,
} from 'lucide-react';
import { useId, type ReactNode } from 'react';
import { t } from '@/core/i18n';
import { DEFAULT_PERIOD, PERIOD_PRESETS, type ReportFilterState } from '@/shared/hooks/useReportFilters';
import type { LookupItem } from '@/shared/models/reporting';

export interface FilterLookups {
  countries: LookupItem[];
  centres: LookupItem[];
  visaTypes: LookupItem[];
}

interface FilterBarProps {
  state: ReportFilterState;
  lookups: FilterLookups;
}

interface FieldProps {
  icon: LucideIcon;
  label: string;
  /** The id of the control this field labels. */
  controlId: string;
  /** True when the field narrows the report, which lights it up. */
  isActive: boolean;
  /** Selects get a chevron; a date input brings its own picker button. */
  hasChevron?: boolean;
  children: ReactNode;
}

/**
 * One filter as a pill: its icon, its label above its value. The control fills the whole pill, so a
 * click anywhere on it opens the choice.
 */
function Field({ icon: Icon, label, controlId, isActive, hasChevron = true, children }: FieldProps) {
  return (
    <div className="field" data-active={isActive}>
      <span className="field__icon" aria-hidden="true">
        <Icon size={16} />
      </span>
      <label className="field__label" htmlFor={controlId}>
        {label}
      </label>
      {children}
      {hasChevron && <ChevronDown className="field__chevron" size={15} aria-hidden="true" />}
    </div>
  );
}

interface SelectFieldProps {
  icon: LucideIcon;
  label: string;
  value: string;
  options: LookupItem[];
  onChange(value: string | null): void;
}

function SelectField({ icon, label, value, options, onChange }: SelectFieldProps) {
  const id = useId();

  return (
    <Field icon={icon} label={label} controlId={id} isActive={value !== ''}>
      <select id={id} className="field__control" value={value} onChange={(event) => onChange(event.target.value || null)}>
        <option value="">{t('filters.all')}</option>
        {options.map((option) => (
          <option key={option.code} value={option.code}>
            {option.name}
          </option>
        ))}
      </select>
    </Field>
  );
}

/** The filters every report shares. Its state is the URL, so it only reads and writes that. */
export function FilterBar({ state, lookups }: FilterBarProps) {
  const periodId = useId();
  const fromId = useId();
  const toId = useId();
  const errorId = useId();
  const { filters } = state;

  // A centre belongs to one country: once a country is chosen, only its centres are offered.
  const centres = filters.countryCode
    ? lookups.centres.filter((centre) => centre.group === filters.countryCode)
    : lookups.centres;

  const problemKey =
    state.problem === 'tooLong' ? 'filters.rangeTooLong' : state.problem === 'order' ? 'filters.rangeOrder' : null;

  const periodIsActive = state.isCustom || filters.lastDays !== DEFAULT_PERIOD;
  const activeCount = [periodIsActive, filters.countryCode, filters.centreCode, filters.visaTypeCode].filter(Boolean).length;

  return (
    <form className="filters" aria-label={t('filters.label')} onSubmit={(event) => event.preventDefault()}>
      <div className="filters__title">
        <span className="filters__mark" aria-hidden="true">
          <SlidersHorizontal size={17} />
        </span>
        <span className="filters__heading">
          <span className="filters__name">{t('filters.title')}</span>
          <span className="filters__count" data-active={activeCount > 0}>
            {activeCount > 0 ? t('filters.activeCount', { count: activeCount }) : t('filters.noneActive')}
          </span>
        </span>
      </div>

      <div className="filters__fields">
        <Field icon={CalendarDays} label={t('filters.period')} controlId={periodId} isActive={periodIsActive}>
          <select
            id={periodId}
            className="field__control"
            value={state.isCustom ? 'custom' : String(filters.lastDays)}
            onChange={(event) => state.setPeriod(event.target.value === 'custom' ? 'custom' : Number(event.target.value))}
          >
            {PERIOD_PRESETS.map((days) => (
              <option key={days} value={days}>
                {t(`filters.period${days}`)}
              </option>
            ))}
            <option value="custom">{t('filters.periodCustom')}</option>
          </select>
        </Field>

        {state.isCustom && (
          <>
            <Field icon={CalendarRange} label={t('filters.from')} controlId={fromId} isActive hasChevron={false}>
              <input
                id={fromId}
                type="date"
                className="field__control"
                value={state.customRange.from}
                aria-invalid={problemKey !== null}
                aria-describedby={problemKey ? errorId : undefined}
                onChange={(event) => state.setCustomRange({ from: event.target.value })}
              />
            </Field>
            <Field icon={CalendarRange} label={t('filters.to')} controlId={toId} isActive hasChevron={false}>
              <input
                id={toId}
                type="date"
                className="field__control"
                value={state.customRange.to}
                aria-invalid={problemKey !== null}
                aria-describedby={problemKey ? errorId : undefined}
                onChange={(event) => state.setCustomRange({ to: event.target.value })}
              />
            </Field>
          </>
        )}

        <SelectField
          icon={Globe2}
          label={t('filters.country')}
          value={filters.countryCode ?? ''}
          options={lookups.countries}
          onChange={state.setCountry}
        />
        <SelectField
          icon={Building2}
          label={t('filters.centre')}
          value={filters.centreCode ?? ''}
          options={centres}
          onChange={state.setCentre}
        />
        <SelectField
          icon={Stamp}
          label={t('filters.visaType')}
          value={filters.visaTypeCode ?? ''}
          options={lookups.visaTypes}
          onChange={state.setVisaType}
        />
      </div>

      <div className="filters__actions">
        <label className="switch" data-active={filters.compare}>
          <GitCompareArrows className="switch__icon" size={16} aria-hidden="true" />
          <span className="switch__text">{t('filters.compare')}</span>
          <input type="checkbox" checked={filters.compare} onChange={(event) => state.setCompare(event.target.checked)} />
          <span className="switch__track" aria-hidden="true" />
        </label>

        <button type="button" className="button filters__reset" onClick={state.reset} disabled={state.isDefault}>
          <RotateCcw size={15} aria-hidden="true" />
          {t('filters.reset')}
        </button>
      </div>

      {problemKey && (
        <p id={errorId} className="field__error" role="alert">
          {t(problemKey)}
        </p>
      )}
    </form>
  );
}
