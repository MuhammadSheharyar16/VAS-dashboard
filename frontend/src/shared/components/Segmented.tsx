import { motion } from 'motion/react';
import { useId, type KeyboardEvent } from 'react';

export interface SegmentedOption<T extends string> {
  value: T;
  label: string;
}

interface SegmentedProps<T extends string> {
  label: string;
  options: SegmentedOption<T>[];
  value: T;
  onChange(value: T): void;
  /** The id of the region the tabs control. */
  controls: string;
}

/** A tab list whose highlight glides to the selected tab. Arrow keys move between tabs. */
export function Segmented<T extends string>({ label, options, value, onChange, controls }: SegmentedProps<T>) {
  const pillId = useId();

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    if (step === 0) {
      return;
    }

    event.preventDefault();
    const index = options.findIndex((option) => option.value === value);
    const next = options[(index + step + options.length) % options.length];
    if (next) {
      onChange(next.value);
      const tabs = event.currentTarget.querySelectorAll<HTMLButtonElement>('[role="tab"]');
      tabs[options.indexOf(next)]?.focus();
    }
  };

  return (
    <div className="segmented" role="tablist" aria-label={label} onKeyDown={handleKeyDown}>
      {options.map((option) => {
        const selected = option.value === value;

        return (
          <button
            key={option.value}
            type="button"
            role="tab"
            className="segmented__item"
            aria-selected={selected}
            aria-controls={controls}
            tabIndex={selected ? 0 : -1}
            onClick={() => onChange(option.value)}
          >
            {selected && (
              <motion.span
                layoutId={pillId}
                className="segmented__pill"
                transition={{ type: 'spring', stiffness: 380, damping: 32 }}
              />
            )}
            <span className="segmented__label">{option.label}</span>
          </button>
        );
      })}
    </div>
  );
}
