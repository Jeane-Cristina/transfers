import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client';
import type { TransferInput, TransferStatus } from './types';

const LIVE_STATUSES: TransferStatus[] = ['Scheduled', 'Processing'];
export const HISTORY_PAGE_SIZE = 15;

export const useDirectory = () =>
  useQuery({ queryKey: ['directory'], queryFn: api.directory, staleTime: 60_000 });

export const useAccount = (id: string) =>
  useQuery({ queryKey: ['accounts', id], queryFn: () => api.getAccount(id) });

export const useAccountUsage = (id: string | undefined) =>
  useQuery({
    queryKey: ['accounts', id, 'usage'],
    queryFn: () => api.getAccountUsage(id!),
    enabled: Boolean(id),
    staleTime: 0,
    refetchInterval: 15_000,
  });

export const useAccountTransfers = (id: string, page: number, pageSize: number = HISTORY_PAGE_SIZE) =>
  useQuery({
    queryKey: ['accounts', id, 'transfers', page, pageSize],
    queryFn: () => api.listAccountTransfers(id, page, pageSize),
    placeholderData: (previous) => previous,
  });

export const useTransfer = (id: string) =>
  useQuery({
    queryKey: ['transfers', id],
    queryFn: () => api.getTransfer(id),
    refetchInterval: (query) =>
      query.state.data && LIVE_STATUSES.includes(query.state.data.status) ? 3000 : false,
  });

export function useSubmitTransfer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (args: { input: TransferInput; scheduledFor?: string; idempotencyKey: string }) =>
      args.scheduledFor
        ? api.scheduleTransfer({ ...args.input, scheduledFor: args.scheduledFor }, args.idempotencyKey)
        : api.createTransfer(args.input, args.idempotencyKey),
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['accounts'] }),
  });
}

export function useCancelTransfer() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: api.cancelTransfer,
    onSuccess: (transfer) => {
      queryClient.setQueryData(['transfers', transfer.id], transfer);
      queryClient.invalidateQueries({ queryKey: ['accounts'] });
    },
  });
}
