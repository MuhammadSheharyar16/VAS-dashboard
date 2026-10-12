import {
  Area,
  CartesianGrid,
  ComposedChart,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
  type TooltipProps,
} from 'recharts';
import { formatCompact, formatNumber } from '@/shared/utils/format';

export interface TrendSeries {
  /** The property of each point that holds this series' value. */
  key: string;
  label: string;
  /** The categorical colour slot, 1 to 8. A series keeps its slot whatever else is shown. */
  slot: 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;
}

export type TrendDatum = { label: string } & Record<string, string | number>;

interface TrendChartProps {
  data: TrendDatum[];
  series: TrendSeries[];
  formatValue?(value: number): string;
  /** A short description of the chart for screen readers. The table view carries the figures. */
  ariaLabel: string;
}

const seriesColour = (slot: number) => `var(--series-${slot})`;

function TrendTooltip({
  active,
  label,
  payload,
  series,
  formatValue,
}: TooltipProps<number, string> & Pick<TrendChartProps, 'series' | 'formatValue'> & { payload?: { dataKey?: string | number; value?: number }[]; label?: string }) {
  if (!active || !payload?.length) {
    return null;
  }

  return (
    <div className="tooltip">
      <p className="tooltip__title">{label}</p>
      {series.map((item) => {
        const entry = payload.find((candidate) => candidate.dataKey === item.key);
        return (
          <p key={item.key} className="tooltip__row">
            <span className="legend__item">
              <span className="legend__swatch" style={{ background: seriesColour(item.slot) }} />
              {item.label}
            </span>
            <strong>{(formatValue ?? formatNumber)(Number(entry?.value ?? 0))}</strong>
          </p>
        );
      })}
    </div>
  );
}

export function Legend({ series }: { series: TrendSeries[] }) {
  // One series needs no legend: the panel title already names it.
  if (series.length < 2) {
    return null;
  }

  return (
    <ul className="legend" style={{ listStyle: 'none', margin: 0, padding: 0 }}>
      {series.map((item) => (
        <li key={item.key} className="legend__item">
          <span className="legend__swatch" style={{ background: seriesColour(item.slot) }} aria-hidden="true" />
          {item.label}
        </li>
      ))}
    </ul>
  );
}

/** Change over time: thin lines on one shared axis, a quiet grid, and a tooltip that follows the pointer. */
export function TrendChart({ data, series, formatValue, ariaLabel }: TrendChartProps) {
  const [first] = series;

  return (
    <div>
      <Legend series={series} />
      <div className="chart" role="img" aria-label={ariaLabel}>
        <ResponsiveContainer width="100%" height="100%">
          <ComposedChart data={data} margin={{ top: 12, right: 8, bottom: 0, left: 0 }}>
            <CartesianGrid vertical={false} stroke="var(--chart-grid)" />
            <XAxis
              dataKey="label"
              tickLine={false}
              axisLine={{ stroke: 'var(--chart-axis)' }}
              minTickGap={28}
              tickMargin={8}
            />
            <YAxis
              width={44}
              tickLine={false}
              axisLine={false}
              tickFormatter={(value: number) => formatCompact(value)}
              allowDecimals={false}
            />
            <Tooltip
              cursor={{ stroke: 'var(--chart-axis)', strokeWidth: 1 }}
              content={<TrendTooltip series={series} formatValue={formatValue} />}
            />
            {first && (
              <Area
                type="monotone"
                dataKey={first.key}
                stroke="none"
                fill="var(--series-1-soft)"
                isAnimationActive={false}
                tooltipType="none"
                legendType="none"
              />
            )}
            {series.map((item) => (
              <Line
                key={item.key}
                type="monotone"
                dataKey={item.key}
                name={item.label}
                stroke={seriesColour(item.slot)}
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 4, stroke: 'var(--chart-surface)', strokeWidth: 2 }}
                animationDuration={900}
              />
            ))}
          </ComposedChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}
