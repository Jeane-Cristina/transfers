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
