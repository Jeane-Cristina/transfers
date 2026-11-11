using Transfers.Domain.Rules;

namespace Transfers.Domain.Accounts;

public sealed record PeriodLimit(decimal MaxAmountPerHour, int MaxAttemptsPerHour);

public sealed record TransferLimits(
    decimal DayMaxAmount,
    int DayMaxAttempts,
    decimal NightMaxAmount,
    int NightMaxAttempts)
{
    public static TransferLimits Default => new(5_000m, 5, 1_000m, 3);

    public PeriodLimit For(TransferPeriod period) => period == TransferPeriod.Night
        ? new PeriodLimit(NightMaxAmount, NightMaxAttempts)
        : new PeriodLimit(DayMaxAmount, DayMaxAttempts);
}
