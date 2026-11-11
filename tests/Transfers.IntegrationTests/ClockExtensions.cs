using Microsoft.Extensions.Time.Testing;

namespace Transfers.IntegrationTests;

public static class ClockExtensions
{
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public static void AdvanceToLocal(this FakeTimeProvider clock, TimeOnly target)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), SaoPaulo);
        var candidate = new DateTimeOffset(nowLocal.Date + target.ToTimeSpan(), nowLocal.Offset);

        if (candidate <= nowLocal)
            candidate = candidate.AddDays(1);

        clock.SetUtcNow(candidate.ToUniversalTime());
    }
}
