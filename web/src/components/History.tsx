import { Link } from 'react-router';
import type { Transfer } from '../api/types';
import { dayKey, dayLabel, shortDateTime, time } from '../lib/format';
import { Money } from './HideValues';
import { Icon } from './Icon';
import { StatusBadge } from './StatusBadge';

export function referenceDate(t: Transfer): string {
  return t.status === 'Scheduled' ? t.createdAt : t.processedAt ?? t.scheduledFor ?? t.createdAt;
}

export function groupByDay(items: Transfer[]) {
  const groups: { key: string; label: string; items: Transfer[] }[] = [];
  for (const t of items) {
    const iso = referenceDate(t);
    const key = dayKey(iso);
    const last = groups[groups.length - 1];
    if (last && last.key === key) last.items.push(t);
    else groups.push({ key, label: dayLabel(iso), items: [t] });
  }
  return groups;
}

interface HistoryListProps {
  items: Transfer[];
  accountId: string;
  names: Map<string, string>;
  busy?: boolean;
}

export function HistoryList({ items, accountId, names, busy }: HistoryListProps) {
  return (
    <div className="history" aria-busy={busy}>
      {groupByDay(items).map((group) => (
        <section key={group.key} className="history__group" aria-label={group.label}>
          <h3 className="history__day">{group.label}</h3>
          <ul>
            {group.items.map((t) => (
              <HistoryRow key={t.id} transfer={t} accountId={accountId} names={names} />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}

function HistoryRow({ transfer: t, accountId, names }: { transfer: Transfer; accountId: string; names: Map<string, string> }) {
  const sent = t.sourceAccountId === accountId;
  const other = names.get(sent ? t.destinationAccountId : t.sourceAccountId) ?? 'Outra conta';
  const settled = t.status === 'Completed';
  const amountClass = settled ? (sent ? 'amount amount--out' : 'amount amount--in') : 'amount amount--muted';
  const verb =
    t.status === 'Cancelled' ? 'Cancelada' : t.status === 'Failed' ? 'Recusada' : sent ? 'Enviada' : 'Recebida';
  const meta =
    t.status === 'Scheduled' && t.scheduledFor
      ? `Agendada para ${shortDateTime(t.scheduledFor)}`
      : `${verb} às ${time(referenceDate(t))}`;

  return (
    <li>
      <Link to={`/transfers/${t.id}`} className="history-row">
        <span className={`history-row__avatar ${sent ? 'is-sent' : 'is-received'}`} aria-hidden="true">
          <Icon name={sent ? 'arrowUpRight' : 'arrowDownLeft'} size={18} />
        </span>
        <span className="history-row__main">
          <strong>{other}</strong>
          <span className="history-row__meta">{meta}</span>
        </span>
        {!settled && (
          <span className="history-row__status">
            <StatusBadge status={t.status} plain />
          </span>
        )}
        <span className={amountClass}>
          <span className="visually-hidden">{sent ? 'Saída de' : 'Entrada de'} </span>
          <Money value={t.amount} sign={sent ? '−' : '+'} />
        </span>
      </Link>
    </li>
  );
}
