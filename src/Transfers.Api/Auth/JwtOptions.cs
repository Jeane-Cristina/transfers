namespace Transfers.Api.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "transfers-api";
    public string Audience { get; init; } = "transfers-web";
    public string Key { get; init; } = default!;
    public int ExpiresMinutes { get; init; } = 60;
}
