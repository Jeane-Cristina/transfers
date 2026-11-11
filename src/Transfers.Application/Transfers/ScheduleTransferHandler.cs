using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Common;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed class ScheduleTransferHandler(
    IAccountRepository accounts,
    ITransferRepository transfers,
    IUnitOfWork unitOfWork,
    IdempotencyGuard idempotency,
    TimeProvider clock)
{
    public Task<TransferExecution> HandleAsync(ScheduleTransferCommand command, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var locked = await accounts.LockForUpdateAsync(
                [command.SourceAccountId, command.DestinationAccountId], token);

            var source = locked.SingleOrDefault(a => a.Id == command.SourceAccountId)
                ?? throw new NotFoundException("account.not_found", "Conta de origem não encontrada.");
            var destination = locked.SingleOrDefault(a => a.Id == command.DestinationAccountId)
                ?? throw new NotFoundException("account.not_found", "Conta de destino não encontrada.");

            var requestHash = RequestHasher.Hash(
                "scheduled", command.SourceAccountId, command.DestinationAccountId,
                command.Amount, command.ScheduledFor);

            var replay = await idempotency.FindReplayAsync(command.IdempotencyKey, requestHash, token);
            if (replay is not null)
                return replay;

            if (!source.IsActive || !destination.IsActive)
                throw new DomainException("account.inactive", "Conta bloqueada ou inativa.");

            var transfer = Transfer.Schedule(
                source.Id, destination.Id, command.Amount, command.ScheduledFor,
                clock.GetUtcNow(), command.IdempotencyKey, requestHash);

            transfers.Add(transfer);
            return new TransferExecution(TransferResponse.From(transfer), Replayed: false);
        }, ct);
}
