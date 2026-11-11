import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { ApiError } from '../api/client';
import { useCancelTransfer, useDirectory, useTransfer } from '../api/hooks';
import type { Transfer, TransferStatus } from '../api/types';
import { useSession } from '../components/Auth';
import { Callout } from '../components/Callout';
import { CopyButton } from '../components/CopyButton';
import { Icon, type IconName } from '../components/Icon';
import { Modal } from '../components/Modal';
import { PageBand } from '../components/PageBand';
import { ErrorState, PageSkeleton } from '../components/States';
import { StatusBadge } from '../components/StatusBadge';
import { Timeline, type TimelineStep } from '../components/Timeline';
import { useToast } from '../components/Toast';
import { dateTime, failureCopy, initials, money, relative, statusLabel } from '../lib/format';
import { useDocumentTitle, useNow } from '../lib/hooks';

const statusIcon: Record<TransferStatus, IconName> = {
  Scheduled: 'clock',
  Processing: 'refresh',
  Completed: 'check',
  Failed: 'x',
  Cancelled: 'x',
};

export function TransferDetailPage() {
  const { id = '' } = useParams();
  const session = useSession();
  const transfer = useTransfer(id);
  const directory = useDirectory();
  const cancel = useCancelTransfer();
  const toast = useToast();
  const now = useNow(15_000);
  const [confirmCancel, setConfirmCancel] = useState(false);
  useDocumentTitle('Comprovante');

  if (transfer.isPending) return <PageSkeleton />;
  if (transfer.isError)
    return (
      <div className="page">
        <ErrorState error={transfer.error} onRetry={() => transfer.refetch()} />
      </div>
    );

  const t = transfer.data;
  const holderOf = (accountId: string) => directory.data?.find((a) => a.id === accountId)?.holderName ?? 'Outra conta';
  const sourceName = holderOf(t.sourceAccountId);
  const destinationName = holderOf(t.destinationAccountId);
  const iSent = t.sourceAccountId === session.accountId;
  const live = t.status === 'Scheduled' || t.status === 'Processing';
  const headline = iSent
    ? t.type === 'Scheduled'
      ? 'Transferência agendada'
      : 'Transferência enviada'
    : 'Transferência recebida';

  const doCancel = () =>
    cancel.mutate(t.id, {
      onSuccess: () => {
        setConfirmCancel(false);
        toast({ tone: 'success', title: 'Agendamento cancelado', description: 'Nenhum valor foi movimentado.' });
      },
      onError: (error) => {
        setConfirmCancel(false);
        transfer.refetch();
        toast({
          tone: 'error',
          title: 'Não foi possível cancelar',
          description:
            error instanceof ApiError && error.status === 409
              ? 'A transferência já entrou em processamento.'
              : error.message,
        });
      },
    });

  return (
    <>
      <PageBand title={headline} back={{ to: '/', label: 'Início' }} compact />

      <div className="page">
        <p className="visually-hidden" aria-live="polite">
          Situação: {statusLabel[t.status]}
        </p>

        <div className="receipt-layout">
          <article className="receipt" aria-labelledby="receipt-amount">
            <header className={`receipt__header receipt__header--${t.status.toLowerCase()}`}>
              <span className="receipt__status-icon" aria-hidden="true">
                <Icon name={statusIcon[t.status]} size={22} />
              </span>
              <StatusBadge status={t.status} />
              <p id="receipt-amount" className="receipt__amount num">
                {money(t.amount)}
              </p>
              <p className="receipt__when">
                {t.status === 'Scheduled' && t.scheduledFor
                  ? `Agendada para ${dateTime(t.scheduledFor)}`
                  : t.processedAt
                    ? dateTime(t.processedAt)
                    : dateTime(t.createdAt)}
              </p>
            </header>

            <div className="receipt__parties">
              <Party label="De" name={iSent ? `${sourceName} (você)` : sourceName} initialsOf={sourceName} />
              <span className="receipt__connector" aria-hidden="true">
                <Icon name="arrowDown" size={16} />
              </span>
              <Party label="Para" name={iSent ? destinationName : `${destinationName} (você)`} initialsOf={destinationName} />
            </div>

            {t.status === 'Failed' && t.failureReason && (
              <div className="receipt__section">
                <Callout tone="error" title={failureCopy[t.failureReason].title}>
                  <p>{failureCopy[t.failureReason].explanation}</p>
                  <p className="callout__hint">{failureCopy[t.failureReason].hint}</p>
                </Callout>
              </div>
            )}
            {t.status === 'Scheduled' && t.scheduledFor && (
              <div className="receipt__section">
                <Callout tone="info" title={`Executa ${relative(t.scheduledFor, now)}`}>
                  Saldo, cheque especial e limites da hora são verificados no momento da execução.
                  {iSent ? ' Até lá, você pode cancelar.' : ''}
                </Callout>
              </div>
            )}
            {t.status === 'Cancelled' && (
              <div className="receipt__section">
                <Callout tone="neutral" title="Agendamento cancelado">
                  Nenhum valor foi movimentado.
                </Callout>
              </div>
            )}

            <dl className="details receipt__details">
              <div>
                <dt>Tipo</dt>
                <dd>{t.type === 'Scheduled' ? 'Agendada' : 'Imediata'}</dd>
              </div>
              <div>
                <dt>Solicitada em</dt>
                <dd>{dateTime(t.createdAt)}</dd>
              </div>
              {t.scheduledFor && (
                <div>
                  <dt>Agendada para</dt>
                  <dd>{dateTime(t.scheduledFor)}</dd>
                </div>
              )}
              {t.processedAt && (
                <div>
                  <dt>{t.status === 'Cancelled' ? 'Cancelada em' : 'Processada em'}</dt>
                  <dd>{dateTime(t.processedAt)}</dd>
                </div>
              )}
              <div>
                <dt>Identificador</dt>
                <dd className="details__id">
                  <code>{t.id}</code>
                </dd>
              </div>
            </dl>

            <footer className="receipt__actions">
              {t.status === 'Scheduled' && iSent && (
                <button type="button" className="btn btn--danger-outline" onClick={() => setConfirmCancel(true)}>
                  <Icon name="x" size={18} />
                  Cancelar agendamento
                </button>
              )}
              <CopyButton value={t.id} label="Copiar identificador" />
              <button type="button" className="copy-btn" onClick={() => window.print()}>
                <Icon name="printer" size={16} />
                <span>Imprimir</span>
              </button>
            </footer>
          </article>

          <aside className="panel receipt-side" aria-labelledby="progress-title">
            <header className="panel__header">
              <h2 id="progress-title">Andamento</h2>
              {live && <span className="live-indicator">Atualizando</span>}
            </header>
            <div className="panel__body">
              <Timeline steps={buildSteps(t, now)} />
            </div>
            <div className="receipt-side__links">
              <Link className="btn btn--secondary btn--block" to="/transfers/new">
                <Icon name="plus" size={18} />
                Nova transferência
              </Link>
              <Link className="btn btn--ghost btn--block" to="/">
                Voltar para o início
              </Link>
            </div>
          </aside>
        </div>
      </div>

      <Modal
        open={confirmCancel}
        title="Cancelar este agendamento?"
        onClose={() => !cancel.isPending && setConfirmCancel(false)}
        footer={
          <>
            <button type="button" className="btn btn--ghost" onClick={() => setConfirmCancel(false)} disabled={cancel.isPending} autoFocus>
              Manter agendamento
            </button>
            <button type="button" className="btn btn--danger" onClick={doCancel} disabled={cancel.isPending}>
              {cancel.isPending ? 'Cancelando…' : 'Cancelar agendamento'}
            </button>
          </>
        }
      >
        <p>
          A transferência de {money(t.amount)} para {destinationName}, marcada para {dateTime(t.scheduledFor)}, não será executada. Essa ação não pode ser desfeita.
        </p>
      </Modal>
    </>
  );
}

function Party({ label, name, initialsOf }: { label: string; name: string; initialsOf: string }) {
  return (
    <div className="party">
      <span className="avatar" aria-hidden="true">{initials(initialsOf)}</span>
      <span className="party__text">
        <small>{label}</small>
        <strong>{name}</strong>
      </span>
    </div>
  );
}

function buildSteps(t: Transfer, now: number): TimelineStep[] {
  const steps: TimelineStep[] = [];

  if (t.type === 'Scheduled') {
    steps.push({ key: 'scheduled', label: 'Agendada', detail: dateTime(t.createdAt), state: 'done' });
    if (t.status === 'Cancelled') {
      steps.push({ key: 'cancelled', label: 'Cancelada', detail: dateTime(t.processedAt), state: 'cancelled' });
      return steps;
    }
    const waiting = t.status === 'Scheduled';
    steps.push({
      key: 'wait',
      label: waiting ? 'Aguardando o horário' : 'Horário atingido',
      detail: waiting && t.scheduledFor ? `${dateTime(t.scheduledFor)}, ${relative(t.scheduledFor, now)}` : dateTime(t.scheduledFor),
      state: waiting ? 'current' : 'done',
    });
  } else {
    steps.push({ key: 'requested', label: 'Solicitada', detail: dateTime(t.createdAt), state: 'done' });
  }

  if (t.status === 'Scheduled') {
    steps.push({ key: 'processing', label: 'Verificação', detail: 'Saldo, cheque especial e limites da hora', state: 'pending' });
    steps.push({ key: 'result', label: 'Conclusão', state: 'pending' });
  } else if (t.status === 'Processing') {
    steps.push({ key: 'processing', label: 'Verificando', detail: 'Saldo, cheque especial e limites da hora', state: 'current' });
    steps.push({ key: 'result', label: 'Conclusão', state: 'pending' });
  } else if (t.status === 'Completed') {
    steps.push({ key: 'processing', label: 'Verificação aprovada', detail: 'Saldo e limites conferidos', state: 'done' });
    steps.push({ key: 'result', label: 'Concluída', detail: dateTime(t.processedAt), state: 'done' });
  } else if (t.status === 'Failed') {
    steps.push({
      key: 'processing',
      label: 'Verificação recusada',
      detail: t.failureReason ? failureCopy[t.failureReason].title : undefined,
      state: 'failed',
    });
  }

  return steps;
}
