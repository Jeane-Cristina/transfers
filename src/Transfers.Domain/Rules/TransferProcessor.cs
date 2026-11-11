using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Domain.Rules;

public static class TransferProcessor
{
    public static void Process(
        Transfer transfer,
        Account source,
        Account destination,
        HourlyUsage usage,
        TransferPeriod period,
        DateTimeOffset now)
    {
        var limit = source.Limits.For(period);

        if (usage.Attempts >= limit.MaxAttemptsPerHour)
        {
            transfer.Fail(FailureReason.HourlyAttemptsExceeded, now);
            return;
        }

        if (!source.IsActive || !destination.IsActive)
        {
            transfer.Fail(FailureReason.AccountInactive, now);
            return;
        }

        if (usage.CompletedAmount + transfer.Amount > limit.MaxAmountPerHour)
        {
            transfer.Fail(FailureReason.HourlyAmountExceeded, now);
            return;
        }

        if (!source.CanDebit(transfer.Amount))
        {
            transfer.Fail(FailureReason.InsufficientFunds, now);
            return;
        }

        source.Debit(transfer.Amount);
        destination.Credit(transfer.Amount);
        transfer.Complete(now);
    }
}
