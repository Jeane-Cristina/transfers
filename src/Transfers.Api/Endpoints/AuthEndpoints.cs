using System.Security.Claims;
using Transfers.Api.Auth;
using Transfers.Application.Abstractions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;

namespace Transfers.Api.Endpoints;

public sealed record LoginRequest(string Document, string Password);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, AccountResponse Account);

public sealed record RegisterRequest(string HolderName, string Document, string Password);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
                LoginRequest request,
                IAuthenticationService authentication,
                JwtTokenService tokens,
                CancellationToken ct) =>
            {
                var account = await authentication.AuthenticateAsync(request.Document ?? "", request.Password ?? "", ct);
                if (account is null)
                    return Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Não foi possível entrar",
                        detail: "Documento ou senha inválidos.",
                        extensions: new Dictionary<string, object?> { ["code"] = "auth.invalid_credentials" });

                var (token, expiresAt) = tokens.Issue(account.Id, account.HolderName);
                return Results.Ok(new LoginResponse(token, expiresAt, AccountResponse.From(account)));
            })
            .AllowAnonymous()
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", async (
                RegisterRequest request,
                IAccountRegistrationService registration,
                JwtTokenService tokens,
                CancellationToken ct) =>
            {
                var account = await registration.RegisterAsync(request.HolderName, request.Document, request.Password, ct);
                var (token, expiresAt) = tokens.Issue(account.Id, account.HolderName);
                return Results.Created("/api/auth/me", new LoginResponse(token, expiresAt, AccountResponse.From(account)));
            })
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/me", async (ClaimsPrincipal user, IAccountRepository accounts, CancellationToken ct) =>
            {
                var account = await accounts.GetByIdAsync(user.AccountId(), ct)
                    ?? throw new NotFoundException("account.not_found", "Conta não encontrada.");
                return Results.Ok(AccountResponse.From(account));
            })
            .RequireAuthorization()
            .Produces<AccountResponse>();

        return app;
    }
}
