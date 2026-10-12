import { Compass, Hourglass, LockKeyhole, Settings2, TriangleAlert } from 'lucide-react';
import { motion } from 'motion/react';
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/core/auth/session';
import { t } from '@/core/i18n';

interface MessageProps {
  icon: ReactNode;
  title: string;
  body: string;
  action?: ReactNode;
  /** Inline messages sit inside the shell; the others fill the page. */
  inline?: boolean;
}

function Message({ icon, title, body, action, inline = false }: MessageProps) {
  const card = (
    <motion.section
      className={`message ${inline ? 'message--inline' : ''}`}
      initial={{ opacity: 0, y: 16, scale: 0.98 }}
      animate={{ opacity: 1, y: 0, scale: 1 }}
      transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1] }}
    >
      <span className="message__icon" aria-hidden="true">
        {icon}
      </span>
      <h1 className="message__title">{title}</h1>
      <p className="message__body">{body}</p>
      {action}
    </motion.section>
  );

  return inline ? card : <main className="fullpage">{card}</main>;
}

const HOME = '/executive-summary';

export function ComingSoonPage({ area }: { area: string }) {
  return (
    <Message
      inline
      icon={<Hourglass size={30} />}
      title={t('comingSoon.title', { area })}
      body={t('comingSoon.body')}
      action={
        <Link className="button button--primary" to={HOME}>
          {t('comingSoon.back')}
        </Link>
      }
    />
  );
}

export function NotFoundPage() {
  return (
    <Message
      inline
      icon={<Compass size={30} />}
      title={t('notFound.title')}
      body={t('notFound.body')}
      action={
        <Link className="button button--primary" to={HOME}>
          {t('notFound.back')}
        </Link>
      }
    />
  );
}

/** Signed in, but not assigned the role. This is not a prompt to sign in again. */
export function UnauthorizedPage() {
  const { signOut } = useAuth();

  return (
    <Message
      icon={<LockKeyhole size={30} />}
      title={t('unauthorized.title')}
      body={t('unauthorized.body')}
      action={
        <button type="button" className="button" onClick={signOut}>
          {t('unauthorized.signOut')}
        </button>
      }
    />
  );
}

export function ConfigErrorPage({ setting }: { setting: string }) {
  return (
    <Message icon={<Settings2 size={30} />} title={t('configError.title')} body={t('configError.body', { setting })} />
  );
}

export function SignInFailedPage() {
  return <Message icon={<TriangleAlert size={30} />} title={t('signIn.failedTitle')} body={t('signIn.failedBody')} />;
}

export function StartingPage() {
  return (
    <main className="fullpage" role="status" aria-live="polite">
      <div className="message">
        <span className="spinner" aria-hidden="true" />
        <p className="message__body">{t('signIn.starting')}</p>
      </div>
    </main>
  );
}
