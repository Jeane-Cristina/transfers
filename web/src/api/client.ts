import { sessionStore } from './session';
import type {
  Account,
  AccountDirectoryEntry,
  HourlyUsage,
  LoginResponse,
  Paged,
  ProblemDetails,
  Transfer,
  TransferInput,
} from './types';

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Erro ${status}`);
    this.status = status;
    this.problem = problem;
  }
}

async function request<T>(path: string, init: RequestInit = {}, authenticated = true): Promise<T> {
  const session = sessionStore.get();
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (authenticated && session) headers.Authorization = `Bearer ${session.token}`;

  let response: Response;
  try {
    response = await fetch(`/api${path}`, { ...init, headers: { ...headers, ...(init.headers as Record<string, string>) } });
  } catch {
    throw new ApiError(0, { detail: 'Sem conexão com a API. Confira se ela está em execução e tente de novo.' });
  }

  if (response.status === 401 && authenticated) {
    sessionStore.set(null);
    throw new ApiError(401, { detail: 'Sua sessão expirou. Entre novamente.' });
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails;
    throw new ApiError(response.status, problem);
  }

  return (await response.json()) as T;
}

export const api = {
  login: (document: string, password: string) =>
    request<LoginResponse>('/auth/login', { method: 'POST', body: JSON.stringify({ document, password }) }, false),
  register: (holderName: string, document: string, password: string) =>
    request<LoginResponse>(
      '/auth/register',
      { method: 'POST', body: JSON.stringify({ holderName, document, password }) },
      false,
    ),
  me: () => request<Account>('/auth/me'),
  directory: () => request<AccountDirectoryEntry[]>('/accounts'),
  getAccount: (id: string) => request<Account>(`/accounts/${id}`),
  getAccountUsage: (id: string) => request<HourlyUsage>(`/accounts/${id}/usage`),
  listAccountTransfers: (id: string, page: number, pageSize: number) =>
    request<Paged<Transfer>>(`/accounts/${id}/transfers?page=${page}&pageSize=${pageSize}`),
  getTransfer: (id: string) => request<Transfer>(`/transfers/${id}`),
  createTransfer: (input: TransferInput, idempotencyKey: string) =>
    request<Transfer>('/transfers', {
      method: 'POST',
      body: JSON.stringify(input),
      headers: { 'Idempotency-Key': idempotencyKey },
    }),
  scheduleTransfer: (input: TransferInput & { scheduledFor: string }, idempotencyKey: string) =>
    request<Transfer>('/transfers/scheduled', {
      method: 'POST',
      body: JSON.stringify(input),
      headers: { 'Idempotency-Key': idempotencyKey },
    }),
  cancelTransfer: (id: string) => request<Transfer>(`/transfers/${id}/cancel`, { method: 'POST' }),
};

export function newIdempotencyKey(): string {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  return Array.from(bytes, (b: number) => b.toString(16).padStart(2, '0')).join('');
}
