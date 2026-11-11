namespace Transfers.Application;

public sealed class TransferRulesOptions
{
    public string TimeZoneId { get; init; } = "America/Sao_Paulo";
    public string NightStart { get; init; } = "20:00";
    public string NightEnd { get; init; } = "06:00";
}
