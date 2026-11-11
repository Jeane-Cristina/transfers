import { useMemo, useState } from 'react';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useNavigate, useSearchParams } from 'react-router';
import { ApiError, newIdempotencyKey } from '../api/client';
import { useAccount, useAccountUsage, useDirectory, useSubmitTransfer } from '../api/hooks';
import type { Account, AccountDirectoryEntry, HourlyUsage } from '../api/types';
import { AmountInput } from '../components/AmountInput';
import { useSession } from '../components/Auth';
import { BalanceGauge } from '../components/BalanceGauge';
import { Callout } from '../components/Callout';
import { Icon, type IconName } from '../components/Icon';
import { Modal } from '../components/Modal';
import { PageBand } from '../components/PageBand';
import { ErrorState, Skeleton } from '../components/States';
import { useToast } from '../components/Toast';
import { dateTime, failureCopy, initials, money, periodLabel, toLocalInputValue } from '../lib/format';
import { useDocumentTitle } from '../lib/hooks';

const schema = z
  .object({
    sourceAccountId: z.string().min(1),
    destinationAccountId: z.string().min(1, 'Escolha quem vai receber.'),
    amountCents: z.number().int().min(1, 'Informe um valor maior que zero.'),
    mode: z.enum(['now', 'schedule']),
    scheduledFor: z.string(),
  })
  .refine((d) => d.sourceAccountId !== d.destinationAccountId, {
    path: ['destinationAccountId'],
    message: 'Escolha outra pessoa para receber.',
  })
  .refine((d) => d.mode === 'now' || (d.scheduledFor !== '' && new Date(d.scheduledFor).getTime() > Date.now()), {
    path: ['scheduledFor'],
    message: 'Escolha uma data e hora no futuro.',
  });

type FormValues = z.infer<typeof schema>;
type CheckTone = 'ok' | 'warn' | 'error' | 'info';
interface Check {
  tone: CheckTone;
  text: string;
}

const checkIcon: Record<CheckTone, IconName> = { ok: 'check', warn: 'info', error: 'alert', info: 'clock' };

export function NewTransferPage() {
  useDocumentTitle('Nova transferência');
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const session = useSession();
  const toast = useToast();
  const account = useAccount(session.accountId);
  const directory = useDirectory();
  const submit = useSubmitTransfer();
  const [idempotencyKey, setIdempotencyKey] = useState(newIdempotencyKey);
  const [review, setReview] = useState<FormValues | null>(null);

  const {
    control,
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      sourceAccountId: session.accountId,
      destinationAccountId: '',
      amountCents: 0,
      mode: params.get('quando') === 'agendar' ? 'schedule' : 'now',
      scheduledFor: '',
    },
  });

  const values = useWatch({ control });
  const mode = values.mode ?? 'now';
  const amount = (values.amountCents ?? 0) / 100;
  const source = account.data;
  const destination = directory.data?.find((a) => a.id === values.destinationAccountId);
  const usage = useAccountUsage(session.accountId);
  const checks = useMemo(
    () => buildChecks(source, destination, amount, mode, usage.data),
    [source, destination, amount, mode, usage.data],
  );
  const blocking = checks.filter((c) => c.tone === 'error');

  const setSchedule = (date: Date) =>
    setValue('scheduledFor', toLocalInputValue(date), { shouldValidate: true, shouldDirty: true });

  const confirm = () => {
    if (!review) return;
    submit.mutate(
      {
        input: {
          sourceAccountId: review.sourceAccountId,
          destinationAccountId: review.destinationAccountId,
          amount: review.amountCents / 100,
        },
        scheduledFor: review.mode === 'schedule' ? new Date(review.scheduledFor).toISOString() : undefined,
        idempotencyKey,
      },
      {
        onSuccess: (transfer) => {
          setReview(null);
          setIdempotencyKey(newIdempotencyKey());
          toast(
            transfer.status === 'Scheduled'
              ? { tone: 'success', title: 'Transferência agendada', description: `Será executada em ${dateTime(transfer.scheduledFor)}.` }
              : { tone: 'success', title: 'Transferência concluída', description: `${money(transfer.amount)} enviados.` },
          );
          navigate(`/transfers/${transfer.id}`);
        },
        onError: (error) => {
          if (error instanceof ApiError && error.status === 422 && error.problem.transferId) {
            setReview(null);
            setIdempotencyKey(newIdempotencyKey());
            toast({
              tone: 'error',
              title: 'Transferência recusada',
              description: error.problem.failureReason ? failureCopy[error.problem.failureReason].title : error.message,
            });
            navigate(`/transfers/${error.problem.transferId}`);
            return;
          }
          if (error instanceof ApiError && error.status >= 400 && error.status < 500) {
            setIdempotencyKey(newIdempotencyKey());
          }
        },
      },
    );
  };

  const closeReview = () => {
    if (submit.isPending) return;
    setReview(null);
    submit.reset();
  };

  if (account.isError || directory.isError)
    return (
      <div className="page">
        <ErrorState
          error={(account.error ?? directory.error)!}
          onRetry={() => {
            account.refetch();
            directory.refetch();
          }}
        />
      </div>
    );

  const minSchedule = toLocalInputValue(new Date(Date.now() + 60_000));
  const recipients = (directory.data ?? []).filter((a) => a.id !== session.accountId);
  const destinationName = (id: string) => directory.data?.find((a) => a.id === id)?.holderName ?? '';
  const submitLabel = mode === 'schedule' ? 'Revisar agendamento' : 'Revisar transferência';

  return (
    <>
      <PageBand title="Nova transferência" back={{ to: '/', label: 'Início' }} compact />
      <div className="page page--with-action-bar">
        <form id="transfer-form" className="transfer-layout" onSubmit={handleSubmit(setReview)} noValidate>
          <div className="panel transfer-form">
            <fieldset className="form-step">
              <legend className="form-step__title">
                <span className="form-step__number" aria-hidden="true">1</span>
                Para quem
              </legend>
              {directory.isPending ? (
                <div className="recipients">
                  <Skeleton height={64} />
                  <Skeleton height={64} />
                </div>
              ) : (
                <div className="recipients">
                  {recipients.map((r) => {
                    const blocked = r.status !== 'Active';
                    const selected = values.destinationAccountId === r.id;
                    return (
                      <label
                        key={r.id}
                        className={`recipient${selected ? ' is-selected' : ''}${blocked ? ' is-disabled' : ''}`}
                      >
                        <input type="radio" value={r.id} disabled={blocked} {...register('destinationAccountId')} />
                        <span className="avatar avatar--sm" aria-hidden="true">{initials(r.holderName)}</span>
                        <span className="recipient__name">{r.holderName}</span>
                        {blocked ? (
                          <span className="recipient__tag">Bloqueada</span>
                        ) : (
                          <span className="recipient__check" aria-hidden="true">
                            <Icon name="check" size={14} />
                          </span>
                        )}
                      </label>
                    );
                  })}
                </div>
              )}
              {errors.destinationAccountId && <p className="field__error">{errors.destinationAccountId.message}</p>}
            </fieldset>

            <div className="form-step">
              <label className="form-step__title" htmlFor="amount">
                <span className="form-step__number" aria-hidden="true">2</span>
                Quanto
              </label>
              <Controller
                control={control}
                name="amountCents"
                render={({ field }) => (
                  <AmountInput
                    id="amount"
                    cents={field.value}
                    onChange={field.onChange}
                    invalid={Boolean(errors.amountCents)}
                    describedBy="amount-help"
                  />
                )}
              />
              <div className="amount-help">
                <p id="amount-help" className={errors.amountCents ? 'field__error' : 'field__help'}>
                  {errors.amountCents?.message ??
                    (source ? `Disponível: ${money(source.availableToTransfer)}, contando o cheque especial.` : 'Os centavos entram automaticamente.')}
                </p>
                {source && source.availableToTransfer > 0 && (
                  <button
                    type="button"
                    className="link-btn"
                    onClick={() =>
                      setValue('amountCents', Math.round(source.availableToTransfer * 100), { shouldValidate: true })
                    }
                  >
                    Usar tudo
                  </button>
                )}
              </div>
            </div>

            <fieldset className="form-step">
              <legend className="form-step__title">
                <span className="form-step__number" aria-hidden="true">3</span>
                Quando
              </legend>
              <div className="segmented">
                <label className={mode === 'now' ? 'is-selected' : undefined}>
                  <input type="radio" value="now" {...register('mode')} />
                  <Icon name="send" size={18} />
                  Agora
                </label>
                <label className={mode === 'schedule' ? 'is-selected' : undefined}>
                  <input type="radio" value="schedule" {...register('mode')} />
                  <Icon name="calendar" size={18} />
                  Agendar
                </label>
              </div>

              {mode === 'schedule' && (
                <div className="schedule">
                  <div className="quick-picks" role="group" aria-label="Atalhos de horário">
                    <button type="button" className="chip" onClick={() => setSchedule(roundUp(new Date(Date.now() + 5 * 60_000)))}>
                      Em 5 minutos
                    </button>
                    <button type="button" className="chip" onClick={() => setSchedule(roundUp(new Date(Date.now() + 60 * 60_000)))}>
                      Em 1 hora
                    </button>
                    <button type="button" className="chip" onClick={() => setSchedule(tomorrowAt(9))}>
                      Amanhã às 9h
                    </button>
                  </div>
                  <div className="field">
                    <label htmlFor="scheduledFor">Data e hora</label>
                    <input
                      id="scheduledFor"
                      type="datetime-local"
                      min={minSchedule}
                      {...register('scheduledFor')}
                      aria-invalid={errors.scheduledFor ? true : undefined}
                      aria-describedby="scheduledFor-help"
                    />
                    <p id="scheduledFor-help" className={errors.scheduledFor ? 'field__error' : 'field__help'}>
                      {errors.scheduledFor?.message ?? 'Horário de Brasília. Você pode cancelar até a execução.'}
                    </p>
                  </div>
                </div>
              )}
            </fieldset>
          </div>

          <aside className="panel summary" aria-labelledby="summary-title">
            <header className="panel__header">
              <h2 id="summary-title">Resumo</h2>
              {usage.data && mode === 'now' && (
                <span className={`period-chip period-chip--${usage.data.period.toLowerCase()}`}>
                  <Icon name={usage.data.period === 'Night' ? 'moon' : 'sun'} size={14} />
                  {periodLabel[usage.data.period]}
                </span>
              )}
            </header>
            <div className="panel__body stack stack--sm">
              <p className={`summary__amount num${amount > 0 ? '' : ' is-empty'}`}>{money(amount)}</p>

              <div className="route">
                <div className="route__party">
                  <span className="avatar avatar--sm" aria-hidden="true">{source ? initials(source.holderName) : ''}</span>
                  <span>
                    <small>De</small>
                    <strong>Você</strong>
                  </span>
                </div>
                <span className="route__arrow" aria-hidden="true">
                  <Icon name="send" size={16} />
                </span>
                <div className="route__party">
                  <span className={`avatar avatar--sm${destination ? '' : ' avatar--empty'}`} aria-hidden="true">
                    {destination ? initials(destination.holderName) : <Icon name="user" size={16} />}
                  </span>
                  <span>
                    <small>Para</small>
                    <strong>{destination?.holderName ?? 'A escolher'}</strong>
                  </span>
                </div>
              </div>

              {source && (
                <div className="summary__gauges">
                  <BalanceGauge label={`Saldo agora: ${money(source.balance)}`} balance={source.balance} overdraftLimit={source.overdraftLimit} size="sm" />
                  {mode === 'now' && amount > 0 && amount <= source.availableToTransfer && (
                    <BalanceGauge
                      label={`Depois da transferência: ${money(source.balance - amount)}`}
                      balance={source.balance - amount}
                      overdraftLimit={source.overdraftLimit}
                      size="sm"
                    />
                  )}
                </div>
              )}

              {checks.length > 0 && (
                <ul className="checks" aria-live="polite">
                  {checks.map((c) => (
                    <li key={c.text} className={`check check--${c.tone}`}>
                      <Icon name={checkIcon[c.tone]} size={16} />
                      <span>{c.text}</span>
                    </li>
                  ))}
                </ul>
              )}

              <button type="submit" className="btn btn--primary btn--block summary__submit">
                {submitLabel}
              </button>
            </div>
          </aside>
        </form>
      </div>

      <div className="action-bar">
        <div className="action-bar__amount">
          <small>{destination ? `Para ${destination.holderName}` : 'Valor'}</small>
          <strong className="num">{money(amount)}</strong>
        </div>
        <button type="submit" form="transfer-form" className="btn btn--primary">
          Revisar
        </button>
      </div>

      <Modal
        open={review !== null}
        title={review?.mode === 'schedule' ? 'Confirmar agendamento' : 'Confirmar transferência'}
        onClose={closeReview}
        footer={
          <>
            <button type="button" className="btn btn--ghost" onClick={closeReview} disabled={submit.isPending}>
              Voltar e ajustar
            </button>
            <button type="button" className="btn btn--primary" onClick={confirm} disabled={submit.isPending} autoFocus>
              {submit.isPending ? 'Enviando…' : review?.mode === 'schedule' ? 'Confirmar agendamento' : 'Confirmar transferência'}
            </button>
          </>
        }
      >
        {review && (
          <div className="stack stack--sm">
            <div className="review">
              <span className="review__label">{review.mode === 'schedule' ? 'Você vai agendar' : 'Você vai transferir'}</span>
              <p className="review__amount num">{money(review.amountCents / 100)}</p>
            </div>
            <dl className="details details--compact">
              <div>
                <dt>Para</dt>
                <dd>{destinationName(review.destinationAccountId)}</dd>
              </div>
              <div>
                <dt>De</dt>
                <dd>{source?.holderName}</dd>
              </div>
              <div>
                <dt>Quando</dt>
                <dd>{review.mode === 'schedule' ? dateTime(new Date(review.scheduledFor).toISOString()) : 'Agora'}</dd>
              </div>
            </dl>
            {blocking.length > 0 && (
              <Callout tone="warn" title="Pelos dados atuais, esta transferência deve ser recusada">
                <ul className="plain-list">
                  {blocking.map((c) => (
                    <li key={c.text}>{c.text}</li>
                  ))}
                </ul>
                Você pode confirmar mesmo assim; a recusa fica registrada e conta como tentativa.
              </Callout>
            )}
            {submit.error && !(submit.error instanceof ApiError && submit.error.status === 422 && submit.error.problem.transferId) && (
              <Callout tone="error" title="Não foi possível enviar">
                {submit.error.message}
              </Callout>
            )}
          </div>
        )}
      </Modal>
    </>
  );
}

function buildChecks(
  source: Account | undefined,
  destination: AccountDirectoryEntry | undefined,
  amount: number,
  mode: 'now' | 'schedule',
  usage: HourlyUsage | undefined,
): Check[] {
  const checks: Check[] = [];
  if (source && source.status !== 'Active') checks.push({ tone: 'error', text: 'Sua conta está bloqueada.' });
  if (destination && destination.status !== 'Active') checks.push({ tone: 'error', text: 'A conta de destino está bloqueada.' });
  if (!source) return checks;

  if (mode === 'schedule') {
    checks.push({ tone: 'info', text: 'Saldo e limites da hora serão verificados no horário agendado.' });
  }
  if (amount <= 0) return checks;

  const positive = Math.max(source.balance, 0);
  if (amount > source.availableToTransfer) {
    checks.push({ tone: mode === 'now' ? 'error' : 'warn', text: `Valor maior que o disponível hoje (${money(source.availableToTransfer)}).` });
  } else if (amount > positive) {
    checks.push({ tone: 'warn', text: `Vai usar ${money(amount - positive)} do cheque especial.` });
  } else {
    checks.push({ tone: 'ok', text: 'Cabe no seu saldo.' });
  }

  if (mode === 'now' && usage) {
    const remaining = Math.max(usage.maxAmountPerHour - usage.usedAmount, 0);
    if (usage.attempts >= usage.maxAttemptsPerHour) {
      checks.push({ tone: 'error', text: `Limite de ${usage.maxAttemptsPerHour} tentativas por hora já atingido.` });
    } else {
      checks.push({ tone: 'ok', text: `${usage.attempts} de ${usage.maxAttemptsPerHour} tentativas usadas na última hora.` });
    }
    if (amount > remaining) {
      checks.push({ tone: 'error', text: `Passa do limite da hora: restam ${money(remaining)} neste período.` });
    } else {
      checks.push({ tone: 'ok', text: `Dentro do limite da hora (restam ${money(remaining)}).` });
    }
  }

  return checks;
}

function roundUp(date: Date): Date {
  const result = new Date(date);
  result.setSeconds(0, 0);
  result.setMinutes(result.getMinutes() + 1);
  return result;
}

function tomorrowAt(hour: number): Date {
  const result = new Date();
  result.setDate(result.getDate() + 1);
  result.setHours(hour, 0, 0, 0);
  return result;
}
