import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { Icon } from './Icon';

interface PageBandProps {
  title: string;
  subtitle?: string;
  back?: { to: string; label: string };
  meta?: ReactNode;
  actions?: ReactNode;
  children?: ReactNode;
  compact?: boolean;
}

export function PageBand({ title, subtitle, back, meta, actions, children, compact }: PageBandProps) {
  return (
    <div className={`band${compact ? ' band--compact' : ''}`}>
      <div className="band__inner">
        {back && (
          <Link className="band__back" to={back.to}>
            <Icon name="chevronLeft" size={18} />
            {back.label}
          </Link>
        )}
        <div className="band__head">
          <div className="band__heading">
            <div className="band__title-row">
              <h1>{title}</h1>
              {meta}
            </div>
            {subtitle && <p className="band__subtitle">{subtitle}</p>}
          </div>
          {actions && <div className="band__actions">{actions}</div>}
        </div>
        {children}
      </div>
    </div>
  );
}

interface FigureProps {
  label: string;
  value: ReactNode;
  tone?: 'negative';
  compact?: boolean;
}

export function Figure({ label, value, tone, compact }: FigureProps) {
  return (
    <div className={`figure${compact ? ' figure--compact' : ''}`}>
      <dt>{label}</dt>
      <dd className={tone === 'negative' ? 'is-negative' : undefined}>{value}</dd>
    </div>
  );
}
