import { useMemo } from 'react';
import { Link } from 'react-router';
import { useAccount, useAccountTransfers, useAccountUsage, useDirectory } from '../api/hooks';
import type { Account, HourlyUsage } from '../api/types';
import { useSession } from '../components/Auth';
import { Callout } from '../components/Callout';
import { HideValuesButton, Money } from '../components/HideValues';
import { ThemeToggle } from '../components/Theme';
import { HistoryList } from '../components/History';
import { Icon, type IconName } from '../components/Icon';
import { EmptyState, ErrorState, ListSkeleton, Skeleton } from '../components/States';
import { firstName, initials, money, periodRange } from '../lib/format';
import { useDocumentTitle } from '../lib/hooks';

const RECENT_COUNT = 5;

export function HomePage() {
  const { accountId: id } = useSession();
  const account = useAccount(id);
  const usage = useAccountUsage(id);
  const recent = useAccountTransfers(id, 1, RECENT_COUNT);
  const directory = useDirectory();
  useDocumentTitle('Início');

  const names = useMemo(
    () => new Map((directory.data ?? []).map((a) => [a.id, a.holderName])),
    [directory.data],
  );

  if (account.isPending) return <HomeSkeleton />;
  if (account.isError)
    return (
      <div className="page">
        <ErrorState error={account.error} onRetry={() => account.refetch()} />
      </div>
    );

  const a = account.data;
  const isActive = a.status === 'Active';

  return (
    <div className="nu">
      <header className="nu-hero">
        <div className="nu-hero__inner">
          <div className="nu-hero__top">
            <span className="nu-avatar" aria-hidden="true">{initials(a.holderName)}</span>
            <div className="nu-hero__icons">
              <ThemeToggle className="nu-icon-btn nu-icon-btn--theme" />
              <HideValuesButton className="nu-icon-btn" />
            </div>
          </div>
          <h1 className="nu-hero__hello">Olá, {firstName(a.holderName)}</h1>
          {a.maskedDocument && <p className="nu-hero__doc">CPF {a.maskedDocument}</p>}
        </div>
      </header>

      <div className="nu-body">
        {!isActive && (
          <div className="nu-notice">
            <Callout tone="error" title="Sua conta está bloqueada">
              Ela não envia nem recebe transferências enquanto estiver bloqueada.
            </Callout>
          </div>
        )}

        <Link to="/extrato" className="nu-section nu-section--link" aria-label="Conta: ver extrato">
          <span className="nu-section__title">
            Conta
            <Icon name="chevronRight" size={20} />
          </span>
          <span className={`nu-balance${a.balance < 0 ? ' is-negative' : ''}`}>
            <Money value={a.balance} />
          </span>
          <span className="nu-muted">
            Disponível para transferir <Money value={a.availableToTransfer} className="nu-strong" />
          </span>
        </Link>

        <nav className="nu-actions" aria-label="Ações">
          <ActionButton to="/transfers/new" icon="send" label="Transferir" disabled={!isActive} />
          <ActionButton to="/transfers/new?quando=agendar" icon="calendar" label="Agendar" disabled={!isActive} />
          <ActionButton to="/extrato" icon="receipt" label="Extrato" />
        </nav>

        {a.overdraftLimit > 0 && <OverdraftSection account={a} />}

        <HourlySection usage={usage.data} loading={usage.isPending} />

        <section className="nu-section nu-section--list" aria-labelledby="recent-title">
          <div className="nu-section__title">
            <h2 id="recent-title">Movimentações</h2>
            {(recent.data?.total ?? 0) > 0 && (
              <Link className="text-link" to="/extrato">
                Ver todas
              </Link>
            )}
          </div>
          {recent.isPending ? (
            <ListSkeleton rows={3} />
          ) : recent.isError ? (
            <ErrorState error={recent.error} onRetry={() => recent.refetch()} />
          ) : recent.data.items.length === 0 ? (
            <EmptyState title="Nenhuma movimentação ainda">
              Quando você enviar ou receber uma transferência, ela aparece aqui.
            </EmptyState>
          ) : (
            <HistoryList items={recent.data.items} accountId={a.id} names={names} />
          )}
        </section>
      </div>
    </div>
  );
}

function ActionButton({ to, icon, label, disabled }: { to: string; icon: IconName; label: string; disabled?: boolean }) {
  const content = (
    <>
      <span className="nu-action__circle">
        <Icon name={icon} size={24} />
      </span>
      {label}
    </>
  );
  if (disabled)
    return (
      <span className="nu-action is-disabled" aria-disabled="true">
        {content}
      </span>
    );
  return (
    <Link className="nu-action" to={to}>
      {content}
    </Link>
  );
}

function OverdraftSection({ account: a }: { account: Account }) {
  const inUse = a.overdraftInUse > 0;
  const pct = Math.min((a.overdraftInUse / a.overdraftLimit) * 100, 100);
  return (
    <section className="nu-section" aria-labelledby="overdraft-title">
      <h2 id="overdraft-title" className="nu-section__title">
        Cheque especial
      </h2>
      <span className="nu-muted">{inUse ? 'Em uso' : 'Disponível'}</span>
      <span className="nu-value">
        <Money value={inUse ? a.overdraftInUse : a.overdraftLimit} />
      </span>
      <span className="nu-bar nu-bar--danger" aria-hidden="true">
        <span style={{ width: `${pct}%` }} />
      </span>
      <span className="nu-muted nu-small">
        {inUse
          ? `De ${money(a.overdraftLimit)} contratados. O próximo crédito cobre esse valor primeiro.`
          : `Limite contratado. Entra em uso só se uma transferência passar do saldo.`}
      </span>
    </section>
  );
}

function HourlySection({ usage, loading }: { usage?: HourlyUsage; loading: boolean }) {
  if (loading || !usage)
    return (
      <section className="nu-section">
        <Skeleton height={96} />
      </section>
    );

  const remaining = Math.max(usage.maxAmountPerHour - usage.usedAmount, 0);
  const attemptsLeft = Math.max(usage.maxAttemptsPerHour - usage.attempts, 0);
  const pct = (remaining / usage.maxAmountPerHour) * 100;
  const isNight = usage.period === 'Night';

  return (
    <section className="nu-section" aria-labelledby="hourly-title">
      <div className="nu-section__title">
        <h2 id="hourly-title">Limite da hora</h2>
        <span className="nu-period">
          <Icon name={isNight ? 'moon' : 'sun'} size={14} />
          {isNight ? 'Noite' : 'Dia'}, {periodRange[usage.period]}
        </span>
      </div>
      <span className="nu-muted">Disponível agora</span>
      <span className="nu-value">
        <Money value={remaining} />
      </span>
      <span className={`nu-bar${pct <= 20 ? ' nu-bar--warn' : ''}`} aria-hidden="true">
        <span style={{ width: `${pct}%` }} />
      </span>
      <span className="nu-muted nu-small">
        {attemptsLeft === 1 ? 'Resta 1 tentativa' : `Restam ${attemptsLeft} tentativas`} de {usage.maxAttemptsPerHour} nos últimos 60 minutos. Recusas também contam.
      </span>
    </section>
  );
}

function HomeSkeleton() {
  return (
    <div className="nu" aria-busy="true" aria-label="Carregando">
      <header className="nu-hero">
        <div className="nu-hero__inner">
          <div className="nu-hero__top">
            <span className="nu-avatar" />
          </div>
          <div className="nu-hero__hello">&nbsp;</div>
        </div>
      </header>
      <div className="nu-body">
        <div className="nu-section">
          <Skeleton width="30%" />
          <div style={{ height: 14 }} />
          <Skeleton width="55%" height={30} />
        </div>
        <ListSkeleton rows={3} />
      </div>
    </div>
  );
}
