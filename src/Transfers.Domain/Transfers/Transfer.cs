using Transfers.Domain.Common;

namespace Transfers.Domain.Transfers;

public sealed class Transfer : AggregateRoot
{
    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public TransferType Type { get; private set; }
    public TransferStatus Status { get; private set; }
    public FailureReason? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? RequestHash { get; private set; }

    private Transfer() { }

    public static Transfer CreateImmediate(
        Guid sourceAccountId, Guid destinationAccountId, decimal amount,
        DateTimeOffset now, string? idempotencyKey = null, string? requestHash = null)
    {
        Validate(sourceAccountId, destinationAccountId, amount);

        return new Transfer
        {
            Id = Guid.CreateVersion7(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Type = TransferType.Immediate,
            Status = TransferStatus.Processing,
            CreatedAt = now,
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash
        };
    }

    public static Transfer Schedule(
        Guid sourceAccountId, Guid destinationAccountId, decimal amount,
        DateTimeOffset scheduledFor, DateTimeOffset now,
        string? idempotencyKey = null, string? requestHash = null)
    {
        Validate(sourceAccountId, destinationAccountId, amount);
        if (scheduledFor <= now)
            throw new DomainException("transfer.schedule_not_in_future", "A data do agendamento deve ser futura.");

        var transfer = new Transfer
        {
            Id = Guid.CreateVersion7(),
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            Amount = amount,
            Type = TransferType.Scheduled,
            Status = TransferStatus.Scheduled,
            CreatedAt = now,
            ScheduledFor = scheduledFor.ToUniversalTime(),
            IdempotencyKey = idempotencyKey,
            RequestHash = requestHash
        };

        transfer.Raise(new TransferScheduled(
            transfer.Id, sourceAccountId, destinationAccountId, amount, transfer.ScheduledFor.Value, now));
        return transfer;
    }

    public void StartProcessing()
    {
        EnsureStatus(TransferStatus.Scheduled, "Só uma transferência agendada pode entrar em processamento.");
        Status = TransferStatus.Processing;
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Processing, "Só uma transferência em processamento pode ser concluída.");
        Status = TransferStatus.Completed;
        ProcessedAt = now;
        Raise(new TransferCompleted(Id, SourceAccountId, DestinationAccountId, Amount, now));
    }

    public void Fail(FailureReason reason, DateTimeOffset now)
    {
        EnsureStatus(TransferStatus.Processing, "Só uma transferência em processamento pode falhar.");
        Status = TransferStatus.Failed;
        FailureReason = reason;
        ProcessedAt = now;
        Raise(new TransferFailed(Id, SourceAccountId, DestinationAccountId, Amount, reason, now));
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status != TransferStatus.Scheduled)
            throw new DomainException("transfer.not_cancellable", "Só transferências agendadas podem ser canceladas.");

        Status = TransferStatus.Cancelled;
        ProcessedAt = now;
        Raise(new TransferCancelled(Id, now));
    }

    private void EnsureStatus(TransferStatus expected, string message)
    {
        if (Status != expected)
            throw new DomainException("transfer.invalid_transition", message);
    }

    private static void Validate(Guid source, Guid destination, decimal amount)
    {
        if (source == destination)
            throw new DomainException("transfer.same_account", "Origem e destino devem ser diferentes.");
        if (amount <= 0)
            throw new DomainException("transfer.invalid_amount", "O valor deve ser maior que zero.");
        if (decimal.Round(amount, 2) != amount)
            throw new DomainException("transfer.invalid_amount", "O valor aceita no máximo duas casas decimais.");
    }
}
