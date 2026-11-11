namespace Transfers.Domain.Rules;

public sealed class TransferPeriodResolver(TimeZoneInfo timeZone, TimeOnly nightStart, TimeOnly nightEnd)
{
    public TransferPeriod Resolve(DateTimeOffset instant)
    {
        var local = TimeZoneInfo.ConvertTime(instant, timeZone);
        var time = TimeOnly.FromTimeSpan(local.TimeOfDay);

        return time.IsBetween(nightStart, nightEnd) ? TransferPeriod.Night : TransferPeriod.Day;
    }
}
