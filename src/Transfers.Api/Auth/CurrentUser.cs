using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Transfers.Application.Common;

namespace Transfers.Api.Auth;

public static class CurrentUser
{
    public static Guid AccountId(this ClaimsPrincipal user)
        => Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id)
            ? id
            : throw new ForbiddenException("auth.invalid_token", "Sessão inválida. Entre novamente.");

    public static void EnsureOwns(this ClaimsPrincipal user, Guid accountId, string message)
    {
        if (user.AccountId() != accountId)
            throw new ForbiddenException("account.forbidden", message);
    }
}
