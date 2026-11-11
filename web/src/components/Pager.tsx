import { Icon } from './Icon';

interface PagerProps {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
  busy?: boolean;
}

export function Pager({ page, totalPages, onChange, busy }: PagerProps) {
  if (totalPages <= 1) return null;
  return (
    <nav className="pager" aria-label="Paginação">
      <button type="button" className="btn btn--ghost btn--sm" disabled={page <= 1 || busy} onClick={() => onChange(page - 1)}>
        <Icon name="chevronLeft" size={18} />
        Anterior
      </button>
      <span className="pager__status num" aria-live="polite">
        Página {page} de {totalPages}
      </span>
      <button type="button" className="btn btn--ghost btn--sm" disabled={page >= totalPages || busy} onClick={() => onChange(page + 1)}>
        Próxima
        <Icon name="chevronRight" size={18} />
      </button>
    </nav>
  );
}
