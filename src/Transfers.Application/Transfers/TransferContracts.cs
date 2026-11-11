using Transfers.Domain.Transfers;

namespace Transfers.Application.Transfers;

public sealed record CreateTransferCommand(
    Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string? IdempotencyKey);

public sealed record TransferResponse(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    TransferType Type,
    TransferStatus Status,
    FailureReason? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? ProcessedAt)
{
    public static TransferResponse From(Transfer t) => new(
        t.Id, t.SourceAccountId, t.DestinationAccountId, t.Amount, t.Type, t.Status,
        t.FailureReason, t.CreatedAt, t.ScheduledFor, t.ProcessedAt);
}

public sealed record TransferExecution(TransferResponse Transfer, bool Replayed);

public sealed record ScheduleTransferCommand(
    Guid SourceAccountId, Guid DestinationAccountId, decimal Amount,
    DateTimeOffset ScheduledFor, string? IdempotencyKey);

public sealed record ExecuteScheduledTransfer(Guid TransferId);
