import { ArrowUpRight, Hourglass } from 'lucide-react';
import { Link } from 'react-router-dom';
import { t } from '@/core/i18n';
import { TiltCard } from '@/shared/components/TiltCard';
import type { HomeArea } from '../useHomeViewModel';

interface AreaCardProps {
  area: HomeArea;
  /** Places from the front of the carousel: 0 at the front, negative to the left, positive to the right. */
  offset: number;
}

/** How the card is lit for where it stands: in full at the front, fading away to its side behind. */
function placeClass(offset: number): string {
  if (offset === 0) {
    return 'area-card--active';
  }

  const far = Math.abs(offset) > 1 ? 'area-card--far' : '';
  return `${offset < 0 ? 'area-card--left' : 'area-card--right'} ${far}`;
}

/** One reporting area as a pane of glass. The whole card is the link that opens the area. */
export function AreaCard({ area, offset }: AreaCardProps) {
  const Icon = area.icon;

  return (
    <TiltCard className="area-card-tilt">
      <Link
        to={area.to}
        className={`area-card ${placeClass(offset)} ${area.isComingSoon ? 'area-card--soon' : ''}`}
        draggable={false}
      >
        <span className="area-card__aura" aria-hidden="true" />
        <span className="area-card__sheen" aria-hidden="true" />

        <span className="area-card__top">
          <span className="area-card__icon" aria-hidden="true">
            <Icon size={26} />
          </span>
          <span className="area-card__position" aria-hidden="true">
            {area.position}
          </span>
        </span>

        <span className="area-card__text">
          <span className="area-card__title">{area.title}</span>
          <span className="area-card__description">{area.description}</span>
        </span>

        <span className="area-card__action">
          {area.isComingSoon ? (
            <span className="area-card__tag">
              <Hourglass size={13} aria-hidden="true" />
              {t('home.comingSoon')}
            </span>
          ) : (
            <span className="area-card__open">
              {t('home.open')}
              <ArrowUpRight size={16} aria-hidden="true" />
            </span>
          )}
        </span>
      </Link>
    </TiltCard>
  );
}
