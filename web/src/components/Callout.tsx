import type { ReactNode } from 'react';
import { Icon, type IconName } from './Icon';

type Tone = 'success' | 'error' | 'info' | 'warn' | 'neutral';

const icons: Record<Tone, IconName> = {
  success: 'check',
  error: 'alert',
  info: 'clock',
  warn: 'alert',
  neutral: 'info',
};

export function Callout({ tone, title, children }: { tone: Tone; title: string; children?: ReactNode }) {
  return (
    <div className={`callout callout--${tone}`} role={tone === 'error' ? 'alert' : undefined}>
      <span className="callout__icon">
        <Icon name={icons[tone]} size={20} />
      </span>
      <div>
        <p className="callout__title">{title}</p>
        {children && <div className="callout__body">{children}</div>}
      </div>
    </div>
  );
}
