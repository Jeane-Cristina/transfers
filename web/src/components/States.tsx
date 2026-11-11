import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { ApiError } from '../api/client';
import { Icon } from './Icon';

export function Skeleton({ width = '100%', height = 16 }: { width?: string | number; height?: number }) {
  return <span className="skeleton" style={{ width, height }} aria-hidden="true" />;
}

export function ListSkeleton({ rows = 4 }: { rows?: number }) {
  return (
    <div className="panel panel--list" aria-busy="true" aria-label="Carregando">
      {Array.from({ length: rows }, (_, i) => (
        <div className="skeleton-row" key={i}>
          <Skeleton width={40} height={40} />
          <div className="skeleton-row__text">
            <Skeleton width="40%" />
            <Skeleton width="25%" height={12} />
          </div>
          <Skeleton width={96} />
        </div>
      ))}
    </div>
  );
}

export function PageSkeleton() {
  return (
    <>
      <div className="band">
        <div className="band__inner" aria-busy="true" aria-label="Carregando">
          <Skeleton width={120} height={14} />
          <div style={{ height: 14 }} />
          <Skeleton width="45%" height={34} />
          <div style={{ height: 28 }} />
          <Skeleton width="70%" height={44} />
        </div>
      </div>
      <div className="page">
        <ListSkeleton rows={3} />
      </div>
    </>
  );
}

export function ErrorState({ error, onRetry }: { error: Error; onRetry?: () => void }) {
  const notFound = error instanceof ApiError && error.status === 404;
  return (
    <div className="state state--error" role="alert">
      <span className="state__icon">
        <Icon name="alert" size={28} />
      </span>
      <h2>{notFound ? 'Não encontramos o que você procura' : 'Não foi possível carregar'}</h2>
      <p>{error.message}</p>
      <div className="state__actions">
        {onRetry && !notFound && (
          <button type="button" className="btn btn--secondary" onClick={onRetry}>
            <Icon name="refresh" size={18} />
            Tentar de novo
          </button>
        )}
        <Link className="btn btn--ghost" to="/">
          Ir para o início
        </Link>
      </div>
    </div>
  );
}

export function EmptyState({ title, children, action }: { title: string; children?: ReactNode; action?: ReactNode }) {
  return (
    <div className="state">
      <span className="state__icon state__icon--neutral">
        <Icon name="wallet" size={28} />
      </span>
      <h2>{title}</h2>
      {children && <p>{children}</p>}
      {action && <div className="state__actions">{action}</div>}
    </div>
  );
}
