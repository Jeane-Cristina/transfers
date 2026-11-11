using Transfers.Domain.Common;

namespace Transfers.Domain.Transfers;

public sealed record TransferScheduled(
    Guid TransferId, Guid SourceAccountId, Guid DestinationAccountId,
    decimal Amount, DateTimeOffset ScheduledFor, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record TransferCompleted(
    Guid TransferId, Guid SourceAccountId, Guid DestinationAccountId,
    decimal Amount, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record TransferFailed(
    Guid TransferId, Guid SourceAccountId, Guid DestinationAccountId,
    decimal Amount, FailureReason Reason, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record TransferCancelled(
    Guid TransferId, DateTimeOffset OccurredAt) : IDomainEvent;
