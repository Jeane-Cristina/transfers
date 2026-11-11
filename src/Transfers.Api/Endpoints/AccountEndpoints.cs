using System.Security.Claims;
using Transfers.Api.Auth;
using Transfers.Application.Abstractions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Accounts;
using Transfers.Domain.Rules;

namespace Transfers.Api.Endpoints;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record AccountDirectoryEntry(Guid Id, string HolderName, AccountStatus Status);

public sealed record HourlyUsageResponse(
    TransferPeriod Period,
    decimal MaxAmountPerHour,
    int MaxAttemptsPerHour,
    decimal UsedAmount,
    int Attempts,
    DateTimeOffset WindowStart);

public static class AccountEndpoints
{
    private const string OwnAccountOnly = "Você só pode consultar a sua própria conta.";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts").RequireAuthorization();

        group.MapGet("/", async (IAccountRepository accounts, CancellationToken ct) =>
                Results.Ok((await accounts.ListAsync(ct))
                    .Select(a => new AccountDirectoryEntry(a.Id, a.HolderName, a.Status))))
            .Produces<IEnumerable<AccountDirectoryEntry>>();

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IAccountRepository accounts, CancellationToken ct) =>
            {
                user.EnsureOwns(id, OwnAccountOnly);
                var account = await accounts.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException("account.not_found", $"Conta {id} não encontrada.");
                return Results.Ok(AccountResponse.From(account));
            })
            .Produces<AccountResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/transfers", async (
                Guid id, ClaimsPrincipal user, ITransferRepository transfers, CancellationToken ct,
                int page = 1, int pageSize = 20) =>
            {
                user.EnsureOwns(id, OwnAccountOnly);
                page = Math.Max(page, 1);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var (items, total) = await transfers.ListByAccountAsync(id, page, pageSize, ct);
                return Results.Ok(new PagedResponse<TransferResponse>(
                    items.Select(TransferResponse.From).ToList(), page, pageSize, total));
            })
            .Produces<PagedResponse<TransferResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}/usage", async (
                Guid id,
                ClaimsPrincipal user,
                IAccountRepository accounts,
                ITransferRepository transfers,
                TransferPeriodResolver periods,
                TimeProvider clock,
                CancellationToken ct) =>
            {
                user.EnsureOwns(id, OwnAccountOnly);
                var account = await accounts.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException("account.not_found", $"Conta {id} não encontrada.");

                var now = clock.GetUtcNow();
                var windowStart = now.AddHours(-1);
                var usage = await transfers.GetHourlyUsageAsync(id, windowStart, ct);
                var period = periods.Resolve(now);
                var limit = account.Limits.For(period);

                return Results.Ok(new HourlyUsageResponse(
                    period, limit.MaxAmountPerHour, limit.MaxAttemptsPerHour,
                    usage.CompletedAmount, usage.Attempts, windowStart));
            })
            .Produces<HourlyUsageResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
