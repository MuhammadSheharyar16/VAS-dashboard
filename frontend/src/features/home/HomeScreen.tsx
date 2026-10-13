import { motion } from 'motion/react';
import { t } from '@/core/i18n';
import type { MenuItem } from '@/features/session/api/sessionRepository';
import { Carousel3D } from '@/shared/components/Carousel3D';
import { AreaCard } from './components/AreaCard';
import { useHomeViewModel, type HomeArea } from './useHomeViewModel';
import './home.css';

const RISE = {
  initial: { opacity: 0, y: 18 },
  animate: { opacity: 1, y: 0 },
};

/** The first screen after sign-in: a greeting and the reporting areas on a 3D carousel. */
export function HomeScreen({ menu }: { menu: MenuItem[] }) {
  const { greetingName, areas } = useHomeViewModel(menu);

  return (
    <div className="home">
      <header className="home__intro">
        <motion.p className="home__eyebrow" {...RISE} transition={{ duration: 0.6, ease: [0.22, 1, 0.36, 1] }}>
          {t('home.eyebrow')}
        </motion.p>
        <motion.h1 className="home__title" {...RISE} transition={{ duration: 0.7, delay: 0.08, ease: [0.22, 1, 0.36, 1] }}>
          {t('home.title', { name: greetingName })}
        </motion.h1>
        <motion.p className="home__subtitle" {...RISE} transition={{ duration: 0.7, delay: 0.16, ease: [0.22, 1, 0.36, 1] }}>
          {t('home.subtitle')}
        </motion.p>
      </header>

      <nav className="home__areas" aria-label={t('nav.label')}>
        <Carousel3D<HomeArea>
          items={areas}
          getKey={(area) => area.code}
          getLabel={(area) => area.title}
          renderItem={(area, { offset }) => <AreaCard area={area} offset={offset} />}
          label={t('nav.label')}
          previousLabel={t('home.previous')}
          nextLabel={t('home.next')}
          showLabel={(area) => t('home.show', { area })}
        />
      </nav>

      <p className="visually-hidden">{t('home.hint')}</p>
    </div>
  );
}
