namespace Transfers.Application.Abstractions;

public interface ICommandOutbox
{
    void EnqueueExecuteScheduled(Guid transferId, DateTimeOffset now);
}
