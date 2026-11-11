using Transfers.Application.Abstractions;
using Transfers.Application.Accounts;
using Transfers.Application.Common;
using Transfers.Application.Transfers;

namespace Transfers.Api.Endpoints;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/accounts").WithTags("Accounts");

        group.MapGet("/", async (IAccountRepository accounts, CancellationToken ct) =>
            Results.Ok((await accounts.ListAsync(ct)).Select(AccountResponse.From)));

        group.MapGet("/{id:guid}", async (Guid id, IAccountRepository accounts, CancellationToken ct) =>
        {
            var account = await accounts.GetByIdAsync(id, ct)
                ?? throw new NotFoundException("account.not_found", $"Conta {id} não encontrada.");
            return Results.Ok(AccountResponse.From(account));
        });

        group.MapGet("/{id:guid}/transfers", async (
            Guid id, ITransferRepository transfers, CancellationToken ct, int page = 1, int pageSize = 20) =>
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var (items, total) = await transfers.ListByAccountAsync(id, page, pageSize, ct);
            return Results.Ok(new PagedResponse<TransferResponse>(
                items.Select(TransferResponse.From).ToList(), page, pageSize, total));
        });

        return app;
    }
}
