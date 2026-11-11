using Transfers.Application.Abstractions;
using Transfers.Application.Transfers;
using Transfers.Infrastructure.Messaging;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.Outbox;

public sealed class EfCommandOutbox(TransfersDbContext db) : ICommandOutbox
{
    public void EnqueueExecuteScheduled(Guid transferId, DateTimeOffset now)
        => db.OutboxMessages.Add(OutboxMessage.Command(
            MessagingTopology.ExecuteScheduledRoutingKey, new ExecuteScheduledTransfer(transferId), now));
}
