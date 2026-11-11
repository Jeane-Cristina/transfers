using Transfers.Application.Abstractions;

namespace Transfers.Application.Transfers;

public sealed class ScheduledTransferDispatcher(
    ITransferRepository transfers,
    ICommandOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public Task<int> DispatchDueAsync(int batchSize, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var now = clock.GetUtcNow();
            var due = await transfers.LockDueScheduledAsync(now, batchSize, token);

            foreach (var transfer in due)
            {
                transfer.StartProcessing();
                outbox.EnqueueExecuteScheduled(transfer.Id, now);
            }

            return due.Count;
        }, ct);
}
