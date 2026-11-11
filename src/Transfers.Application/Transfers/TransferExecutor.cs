using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Domain.Accounts;
using Transfers.Domain.Rules;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed class TransferExecutor(
    IAccountRepository accounts,
    ITransferRepository transfers,
    IUnitOfWork unitOfWork,
    IdempotencyGuard idempotency,
    TransferPeriodResolver periods,
    TimeProvider clock)
{
    public Task<TransferExecution> ExecuteImmediateAsync(CreateTransferCommand command, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var (source, destination) = await LockAccountsAsync(
                command.SourceAccountId, command.DestinationAccountId, token);

            var requestHash = RequestHasher.Hash(
                "immediate", command.SourceAccountId, command.DestinationAccountId, command.Amount);

            var replay = await idempotency.FindReplayAsync(command.IdempotencyKey, requestHash, token);
            if (replay is not null)
                return replay;

            var now = clock.GetUtcNow();
            var transfer = Transfer.CreateImmediate(
                source.Id, destination.Id, command.Amount, now, command.IdempotencyKey, requestHash);

            await ProcessAsync(transfer, source, destination, now, token);
            transfers.Add(transfer);

            return new TransferExecution(TransferResponse.From(transfer), Replayed: false);
        }, ct);

    public Task ExecuteScheduledAsync(Guid transferId, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var transfer = await transfers.LockForUpdateAsync(transferId, token);

            if (transfer is null || transfer.Status != TransferStatus.Processing)
                return;

            var (source, destination) = await LockAccountsAsync(
                transfer.SourceAccountId, transfer.DestinationAccountId, token);

            await ProcessAsync(transfer, source, destination, clock.GetUtcNow(), token);
        }, ct);

    private async Task ProcessAsync(
        Transfer transfer, Account source, Account destination, DateTimeOffset now, CancellationToken ct)
    {
        var usage = await transfers.GetHourlyUsageAsync(source.Id, now.AddHours(-1), ct);
        var period = periods.Resolve(now);

        TransferProcessor.Process(transfer, source, destination, usage, period, now);
    }

    private async Task<(Account Source, Account Destination)> LockAccountsAsync(
        Guid sourceId, Guid destinationId, CancellationToken ct)
    {
        var locked = await accounts.LockForUpdateAsync([sourceId, destinationId], ct);

        var source = locked.SingleOrDefault(a => a.Id == sourceId)
            ?? throw new NotFoundException("account.not_found", $"Conta de origem {sourceId} não encontrada.");
        var destination = locked.SingleOrDefault(a => a.Id == destinationId)
            ?? throw new NotFoundException("account.not_found", $"Conta de destino {destinationId} não encontrada.");

        return (source, destination);
    }
}
