using Transfers.Domain.Rules;

namespace Transfers.UnitTests;

public class TransferPeriodResolverTests
{
    private static readonly TransferPeriodResolver Resolver = new(
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"),
        new TimeOnly(20, 0),
        new TimeOnly(6, 0));

    [Theory]
    [InlineData("2026-11-10T22:59:00Z", TransferPeriod.Day)]
    [InlineData("2026-11-10T23:00:00Z", TransferPeriod.Night)]
    [InlineData("2026-11-11T03:00:00Z", TransferPeriod.Night)]
    [InlineData("2026-11-11T08:59:00Z", TransferPeriod.Night)]
    [InlineData("2026-11-11T09:00:00Z", TransferPeriod.Day)]
    public void Resolve_periodo_no_fuso_de_Sao_Paulo(string utc, TransferPeriod expected)
    {
        Assert.Equal(expected, Resolver.Resolve(DateTimeOffset.Parse(utc)));
    }
}
