import { Cell, Pie, PieChart, ResponsiveContainer } from 'recharts';
import { t } from '@/core/i18n';
import { formatNumber, formatPercent } from '@/shared/utils/format';

export interface DonutSlice {
  key: string;
  label: string;
  value: number;
  sharePercent: number | null;
  /** The categorical colour slot, 1 to 8. A category keeps its slot on every chart it appears in. */
  slot?: number;
}

interface DonutShareProps {
  slices: DonutSlice[];
  ariaLabel: string;
}

/**
 * Parts of a whole, for up to four parts. The legend beside it lists every count and share,
 * so the ring is never the only way to read the figures.
 */
export function DonutShare({ slices, ariaLabel }: DonutShareProps) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);
  const colourOf = (index: number) => `var(--series-${slices[index]?.slot ?? (index % 8) + 1})`;

  return (
    <div className="donut">
      <div className="donut__chart" role="img" aria-label={ariaLabel}>
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={slices}
              dataKey="value"
              nameKey="label"
              innerRadius="68%"
              outerRadius="100%"
              paddingAngle={2}
              stroke="var(--chart-surface)"
              strokeWidth={2}
              startAngle={90}
              endAngle={-270}
              animationDuration={900}
            >
              {slices.map((slice, index) => (
                <Cell key={slice.key} fill={colourOf(index)} />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>
        <div className="donut__centre">
          <span className="donut__total">{formatNumber(total)}</span>
          <span className="donut__caption">{t('chart.total')}</span>
        </div>
      </div>

      <ul className="donut__legend">
        {slices.map((slice, index) => (
          <li key={slice.key}>
            <span className="legend__swatch" style={{ background: colourOf(index) }} aria-hidden="true" />
            <span>{slice.label}</span>
            <strong>{formatNumber(slice.value)}</strong>
            <span className="donut__share">{formatPercent(slice.sharePercent)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
