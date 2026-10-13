import { BarChart3, Table2 } from 'lucide-react';
import { motion } from 'motion/react';
import { useId, useState, type ReactNode } from 'react';
import { t } from '@/core/i18n';

/**
 * How a block arrives: it fades in while it drifts up and grows to full size, in one unbroken
 * movement that slows to a stop. There is no swing and no bounce, so nothing settles twice.
 */
const ARRIVE = {
  hidden: { opacity: 0, y: 26, scale: 0.955 },
  shown: { opacity: 1, y: 0, scale: 1 },
};

const ARRIVE_EASE = { duration: 0.8, ease: [0.16, 1, 0.3, 1] } as const;

interface PanelProps {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}

/** A titled glass surface. Panels rise into place one after another as a report appears. */
export function Panel({ title, subtitle, actions, children, className = '' }: PanelProps) {
  const headingId = useId();

  return (
    <motion.section
      className={`panel ${className}`}
      aria-labelledby={headingId}
      variants={ARRIVE}
      transition={ARRIVE_EASE}
    >
      <header className="panel__header">
        <div>
          <h2 id={headingId} className="panel__title">
            {title}
          </h2>
          {subtitle && <p className="panel__subtitle">{subtitle}</p>}
        </div>
        {actions}
      </header>
      <div className="panel__body">{children}</div>
    </motion.section>
  );
}

interface ChartPanelProps extends Omit<PanelProps, 'actions' | 'children'> {
  chart: ReactNode;
  /** The same figures as a table, for anyone who cannot or would rather not read the chart. */
  table: ReactNode;
}

export function ChartPanel({ chart, table, ...panel }: ChartPanelProps) {
  const [showTable, setShowTable] = useState(false);
  const Icon = showTable ? BarChart3 : Table2;

  return (
    <Panel
      {...panel}
      actions={
        <button
          type="button"
          className="button button--ghost"
          aria-pressed={showTable}
          onClick={() => setShowTable((current) => !current)}
        >
          <Icon size={15} aria-hidden="true" />
          {t(showTable ? 'chart.showChart' : 'chart.showTable')}
        </button>
      }
    >
      {showTable ? table : chart}
    </Panel>
  );
}

/** Wraps a report so its panels animate in sequence. */
export function Stagger({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <motion.div
      className={className}
      initial="hidden"
      animate="shown"
      variants={{ shown: { transition: { staggerChildren: 0.055 } } }}
    >
      {children}
    </motion.div>
  );
}

export function StaggerItem({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <motion.div
      className={className}
      variants={ARRIVE}
      transition={ARRIVE_EASE}
    >
      {children}
    </motion.div>
  );
}
