import type { AccountStatus, TransferStatus } from '../api/types';
import { accountStatusLabel, statusLabel } from '../lib/format';

export function StatusBadge({ status, onDark, plain }: { status: TransferStatus; onDark?: boolean; plain?: boolean }) {
  return (
    <span className={`badge badge--${status.toLowerCase()}${onDark ? ' badge--on-dark' : ''}${plain ? ' badge--plain' : ''}`}>
      <span className="badge__dot" aria-hidden="true" />
      {statusLabel[status]}
    </span>
  );
}

export function AccountStatusChip({ status, onDark, plain }: { status: AccountStatus; onDark?: boolean; plain?: boolean }) {
  const tone = status === 'Active' ? 'completed' : status === 'Blocked' ? 'failed' : 'cancelled';
  return (
    <span className={`badge badge--${tone}${onDark ? ' badge--on-dark' : ''}${plain ? ' badge--plain' : ''}`}>
      <span className="badge__dot" aria-hidden="true" />
      {accountStatusLabel[status]}
    </span>
  );
}
