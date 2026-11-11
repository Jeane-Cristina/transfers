import type { AccountStatus, FailureReason, TransferPeriod, TransferStatus } from '../api/types';

export const TIME_ZONE = 'America/Sao_Paulo';

const brl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const decimal = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const dateTimeFormat = new Intl.DateTimeFormat('pt-BR', {
  timeZone: TIME_ZONE,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});
const timeFormat = new Intl.DateTimeFormat('pt-BR', { timeZone: TIME_ZONE, hour: '2-digit', minute: '2-digit' });
const relativeFormat = new Intl.RelativeTimeFormat('pt-BR', { numeric: 'auto' });
const dayKeyFormat = new Intl.DateTimeFormat('en-CA', { timeZone: TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit' });
const dayLabelFormat = new Intl.DateTimeFormat('pt-BR', { timeZone: TIME_ZONE, day: 'numeric', month: 'long' });
const shortDateTimeFormat = new Intl.DateTimeFormat('pt-BR', {
  timeZone: TIME_ZONE,
  day: '2-digit',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit',
});

export const money = (value: number) => brl.format(value).replace('-', '\u2212');
export const decimalAmount = (value: number) => decimal.format(value);
export const dateTime = (iso: string | null | undefined) => (iso ? dateTimeFormat.format(new Date(iso)) : '—');
export const time = (iso: string) => timeFormat.format(new Date(iso));
export const shortDateTime = (iso: string) => shortDateTimeFormat.format(new Date(iso)).replace('.', '');
export const dayKey = (iso: string) => dayKeyFormat.format(new Date(iso));

export function dayLabel(iso: string, now: number = Date.now()): string {
  const key = dayKey(iso);
  if (key === dayKey(new Date(now).toISOString())) return 'Hoje';
  if (key === dayKey(new Date(now - 86_400_000).toISOString())) return 'Ontem';
  return dayLabelFormat.format(new Date(iso));
}

export const firstName = (name: string) => name.trim().split(/\s+/)[0] ?? name;

export function relative(iso: string, now: number = Date.now()): string {
  const diff = new Date(iso).getTime() - now;
  const abs = Math.abs(diff);
  if (abs < 60_000) return relativeFormat.format(Math.round(diff / 1000), 'second');
  if (abs < 3_600_000) return relativeFormat.format(Math.round(diff / 60_000), 'minute');
  if (abs < 86_400_000) return relativeFormat.format(Math.round(diff / 3_600_000), 'hour');
  return relativeFormat.format(Math.round(diff / 86_400_000), 'day');
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/);
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase();
}

export const statusLabel: Record<TransferStatus, string> = {
  Scheduled: 'Agendada',
  Processing: 'Processando',
  Completed: 'Concluída',
  Failed: 'Recusada',
  Cancelled: 'Cancelada',
};

export const accountStatusLabel: Record<AccountStatus, string> = {
  Active: 'Ativa',
  Blocked: 'Bloqueada',
  Inactive: 'Inativa',
};

export const periodLabel: Record<TransferPeriod, string> = {
  Day: 'Período diurno',
  Night: 'Período noturno',
};

export const periodRange: Record<TransferPeriod, string> = {
  Day: '6h às 20h',
  Night: '20h às 6h',
};

export const failureCopy: Record<FailureReason, { title: string; explanation: string; hint: string }> = {
  InsufficientFunds: {
    title: 'Saldo insuficiente',
    explanation: 'No momento da tentativa, o valor era maior que o saldo somado ao cheque especial da conta de origem.',
    hint: 'Envie um valor menor ou aguarde um crédito na conta.',
  },
  AccountInactive: {
    title: 'Conta bloqueada',
    explanation: 'A conta de origem ou a de destino estava bloqueada no momento da tentativa.',
    hint: 'Transferências só acontecem entre contas ativas.',
  },
  HourlyAmountExceeded: {
    title: 'Limite de valor da hora',
    explanation: 'Somado ao que já foi transferido nos últimos 60 minutos, o valor passava do limite do período.',
    hint: 'Envie um valor menor ou aguarde a janela de uma hora liberar.',
  },
  HourlyAttemptsExceeded: {
    title: 'Limite de tentativas da hora',
    explanation: 'A conta atingiu o número máximo de tentativas nos últimos 60 minutos. Tentativas recusadas também contam.',
    hint: 'Aguarde a janela de uma hora liberar para tentar de novo.',
  },
};

export function toLocalInputValue(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export function currentPeriod(now: Date = new Date()): TransferPeriod {
  const hour = Number(
    new Intl.DateTimeFormat('en-US', { timeZone: TIME_ZONE, hour: '2-digit', hour12: false }).format(now),
  ) % 24;
  return hour >= 20 || hour < 6 ? 'Night' : 'Day';
}
