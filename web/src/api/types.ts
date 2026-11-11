export type AccountStatus = 'Active' | 'Blocked' | 'Inactive';
export type TransferStatus = 'Scheduled' | 'Processing' | 'Completed' | 'Failed' | 'Cancelled';
export type TransferType = 'Immediate' | 'Scheduled';
export type TransferPeriod = 'Day' | 'Night';
export type FailureReason =
  | 'InsufficientFunds'
  | 'AccountInactive'
  | 'HourlyAmountExceeded'
  | 'HourlyAttemptsExceeded';

export interface TransferLimits {
  dayMaxAmount: number;
  dayMaxAttempts: number;
  nightMaxAmount: number;
  nightMaxAttempts: number;
}

export interface Account {
  id: string;
  holderName: string;
  maskedDocument: string;
  balance: number;
  overdraftLimit: number;
  overdraftInUse: number;
  availableToTransfer: number;
  status: AccountStatus;
  limits: TransferLimits;
}

export interface Transfer {
  id: string;
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
  type: TransferType;
  status: TransferStatus;
  failureReason: FailureReason | null;
  createdAt: string;
  scheduledFor: string | null;
  processedAt: string | null;
}

export interface HourlyUsage {
  period: TransferPeriod;
  maxAmountPerHour: number;
  maxAttemptsPerHour: number;
  usedAmount: number;
  attempts: number;
  windowStart: string;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  transferId?: string;
  failureReason?: FailureReason;
}

export interface TransferInput {
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
}

export interface AccountDirectoryEntry {
  id: string;
  holderName: string;
  status: AccountStatus;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  account: Account;
}
