using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Transfers.Api.Auth;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Api.Endpoints;

public sealed record CreateTransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);

public sealed record ScheduleTransferRequest(
    Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, DateTimeOffset ScheduledFor);

public static class TransferEndpoints
{
    private const string OwnSourceOnly = "Você só pode transferir a partir da sua própria conta.";

    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transfers").WithTags("Transfers").RequireAuthorization();

        group.MapPost("/", CreateAsync)
            .Produces<TransferResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/scheduled", ScheduleAsync)
            .Produces<TransferResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/cancel", CancelAsync)
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetAsync)
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        ClaimsPrincipal user,
        TransferExecutor executor,
        HttpContext http,
        CancellationToken ct)
    {
        user.EnsureOwns(request.SourceAccountId, OwnSourceOnly);

        var execution = await executor.ExecuteImmediateAsync(
            new CreateTransferCommand(request.SourceAccountId, request.DestinationAccountId, request.Amount, idempotencyKey),
            ct);

        if (execution.Replayed)
            http.Response.Headers["Idempotent-Replayed"] = "true";

        return ToHttpResult(execution.Transfer);
    }

    private static async Task<IResult> ScheduleAsync(
        ScheduleTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        ClaimsPrincipal user,
        ScheduleTransferHandler handler,
        HttpContext http,
        CancellationToken ct)
    {
        user.EnsureOwns(request.SourceAccountId, OwnSourceOnly);

        var execution = await handler.HandleAsync(new ScheduleTransferCommand(
            request.SourceAccountId, request.DestinationAccountId, request.Amount,
            request.ScheduledFor, idempotencyKey), ct);

        if (execution.Replayed)
            http.Response.Headers["Idempotent-Replayed"] = "true";

        return Results.Created($"/api/transfers/{execution.Transfer.Id}", execution.Transfer);
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        ClaimsPrincipal user,
        ITransferRepository transfers,
        CancelTransferHandler handler,
        CancellationToken ct)
    {
        var transfer = await transfers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("transfer.not_found", $"Transferência {id} não encontrada.");

        user.EnsureOwns(transfer.SourceAccountId, "Só quem agendou a transferência pode cancelá-la.");

        return Results.Ok(await handler.HandleAsync(id, ct));
    }

    private static async Task<IResult> GetAsync(Guid id, ClaimsPrincipal user, ITransferRepository transfers, CancellationToken ct)
    {
        var transfer = await transfers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("transfer.not_found", $"Transferência {id} não encontrada.");

        var me = user.AccountId();
        if (transfer.SourceAccountId != me && transfer.DestinationAccountId != me)
            throw new ForbiddenException("transfer.forbidden", "Esta transferência não envolve a sua conta.");

        return Results.Ok(TransferResponse.From(transfer));
    }

    internal static IResult ToHttpResult(TransferResponse result) => result.Status switch
    {
        TransferStatus.Failed => Results.Problem(
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Transferência recusada",
            detail: $"Motivo: {result.FailureReason}",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "transfer.rejected",
                ["transferId"] = result.Id,
                ["failureReason"] = result.FailureReason?.ToString()
            }),
        _ => Results.Created($"/api/transfers/{result.Id}", result)
    };
}
