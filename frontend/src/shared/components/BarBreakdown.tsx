import { motion } from 'motion/react';

export interface BarItem {
  key: string;
  name: string;
  value: number;
  /** The value as it should be read, for example "1,204" or "AED 12,300". */
  valueLabel: string;
  /** A quieter note after the value, for example a share. */
  note?: string;
  /** A second, smaller measure drawn inside the bar (the "completed" part of "reached"). */
  secondaryValue?: number;
}

interface BarBreakdownProps {
  items: BarItem[];
  /** Bars are scaled to this. Left out, the largest value fills the track. */
  max?: number;
  ariaLabel: string;
}

/**
 * Magnitude by category, largest first. One measure, so one colour; every bar carries its own
 * value as text, so nothing depends on reading colour or length alone.
 */
export function BarBreakdown({ items, max, ariaLabel }: BarBreakdownProps) {
  const scale = max ?? Math.max(...items.map((item) => item.value), 0);
  const widthOf = (value: number) => (scale <= 0 ? 0 : Math.min(100, (value / scale) * 100));

  return (
    <ul className="bars" aria-label={ariaLabel}>
      {items.map((item, index) => (
        <li key={item.key} className="bars__row">
          <span className="bars__name" title={item.name}>
            {item.name}
          </span>
          <span className="bars__track" aria-hidden="true">
            <motion.span
              className="bars__fill"
              initial={{ width: 0 }}
              animate={{ width: `${widthOf(item.value)}%` }}
              transition={{ duration: 0.8, delay: index * 0.04, ease: [0.22, 1, 0.36, 1] }}
            />
            {item.secondaryValue !== undefined && (
              <motion.span
                className="bars__fill bars__fill--secondary"
                initial={{ width: 0 }}
                animate={{ width: `${widthOf(item.secondaryValue)}%` }}
                transition={{ duration: 0.8, delay: 0.15 + index * 0.04, ease: [0.22, 1, 0.36, 1] }}
              />
            )}
          </span>
          <span className="bars__value">
            {item.valueLabel}
            {item.note && <span className="bars__note">{item.note}</span>}
          </span>
        </li>
      ))}
    </ul>
  );
}
