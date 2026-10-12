import { RotateCcw } from 'lucide-react';
import { useId } from 'react';
import { t } from '@/core/i18n';
import { PERIOD_PRESETS, type ReportFilterState } from '@/shared/hooks/useReportFilters';
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

interface SelectFieldProps {
  label: string;
  value: string;
  options: LookupItem[];
  onChange(value: string | null): void;
}

function SelectField({ label, value, options, onChange }: SelectFieldProps) {
  const id = useId();

  return (
    <div className="field">
      <label className="field__label" htmlFor={id}>
        {label}
      </label>
      <select id={id} className="field__control" value={value} onChange={(event) => onChange(event.target.value || null)}>
        <option value="">{t('filters.all')}</option>
        {options.map((option) => (
          <option key={option.code} value={option.code}>
            {option.name}
          </option>
        ))}
      </select>
    </div>
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

  return (
    <form className="filters" aria-label={t('filters.label')} onSubmit={(event) => event.preventDefault()}>
      <div className="field">
        <label className="field__label" htmlFor={periodId}>
          {t('filters.period')}
        </label>
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
      </div>

      {state.isCustom && (
        <>
          <div className="field">
            <label className="field__label" htmlFor={fromId}>
              {t('filters.from')}
            </label>
            <input
              id={fromId}
              type="date"
              className="field__control"
              value={state.customRange.from}
              aria-invalid={problemKey !== null}
              aria-describedby={problemKey ? errorId : undefined}
              onChange={(event) => state.setCustomRange({ from: event.target.value })}
            />
          </div>
          <div className="field">
            <label className="field__label" htmlFor={toId}>
              {t('filters.to')}
            </label>
            <input
              id={toId}
              type="date"
              className="field__control"
              value={state.customRange.to}
              aria-invalid={problemKey !== null}
              aria-describedby={problemKey ? errorId : undefined}
              onChange={(event) => state.setCustomRange({ to: event.target.value })}
            />
          </div>
        </>
      )}

      <SelectField
        label={t('filters.country')}
        value={filters.countryCode ?? ''}
        options={lookups.countries}
        onChange={state.setCountry}
      />
      <SelectField
        label={t('filters.centre')}
        value={filters.centreCode ?? ''}
        options={centres}
        onChange={state.setCentre}
      />
      <SelectField
        label={t('filters.visaType')}
        value={filters.visaTypeCode ?? ''}
        options={lookups.visaTypes}
        onChange={state.setVisaType}
      />

      <label className="switch">
        <input type="checkbox" checked={filters.compare} onChange={(event) => state.setCompare(event.target.checked)} />
        <span className="switch__track" aria-hidden="true" />
        {t('filters.compare')}
      </label>

      <span className="filters__spacer" />

      <button type="button" className="button button--ghost" onClick={state.reset} disabled={state.isDefault}>
        <RotateCcw size={15} aria-hidden="true" />
        {t('filters.reset')}
      </button>

      {problemKey && (
        <p id={errorId} className="field__error" role="alert">
          {t(problemKey)}
        </p>
      )}
    </form>
  );
}
