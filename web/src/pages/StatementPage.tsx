import { useMemo, useState } from 'react';
import { Link } from 'react-router';
import { useAccountTransfers, useDirectory } from '../api/hooks';
import { useSession } from '../components/Auth';
import { HistoryList } from '../components/History';
import { PageBand } from '../components/PageBand';
import { Pager } from '../components/Pager';
import { EmptyState, ErrorState, ListSkeleton } from '../components/States';
import { useDocumentTitle } from '../lib/hooks';

export function StatementPage() {
  useDocumentTitle('Extrato');
  const { accountId } = useSession();
  const [page, setPage] = useState(1);
  const history = useAccountTransfers(accountId, page);
  const directory = useDirectory();

  const names = useMemo(
    () => new Map((directory.data ?? []).map((a) => [a.id, a.holderName])),
    [directory.data],
  );

  const totalPages = history.data ? Math.max(1, Math.ceil(history.data.total / history.data.pageSize)) : 1;

  return (
    <>
      <PageBand
        title="Extrato"
        subtitle="Tudo o que saiu, entrou ou está agendado na sua conta."
        back={{ to: '/', label: 'Início' }}
        compact
      />
      <div className="page page--narrow">
        <section className="panel" aria-label="Movimentações">
          {history.isPending ? (
            <ListSkeleton rows={6} />
          ) : history.isError ? (
            <ErrorState error={history.error} onRetry={() => history.refetch()} />
          ) : history.data.items.length === 0 ? (
            <EmptyState
              title="Nenhuma movimentação ainda"
              action={
                <Link className="btn btn--secondary" to="/transfers/new">
                  Fazer uma transferência
                </Link>
              }
            >
              Quando você enviar ou receber uma transferência, ela aparece aqui.
            </EmptyState>
          ) : (
            <>
              <HistoryList items={history.data.items} accountId={accountId} names={names} busy={history.isFetching} />
              <Pager
                page={page}
                totalPages={totalPages}
                onChange={(next) => {
                  setPage(next);
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
                busy={history.isFetching}
              />
            </>
          )}
        </section>
      </div>
    </>
  );
}
