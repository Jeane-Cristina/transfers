using Microsoft.AspNetCore.Mvc;
using Transfers.Application.Abstractions;
using Transfers.Application.Common;
using Transfers.Application.Transfers;
using Transfers.Domain.Transfers;

namespace Transfers.Api.Endpoints;

public sealed record CreateTransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/transfers").WithTags("Transfers");

        group.MapPost("/", CreateAsync)
            .Produces<TransferResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}", GetAsync)
            .Produces<TransferResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateTransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        TransferExecutor executor,
        CancellationToken ct)
    {
        var result = await executor.ExecuteImmediateAsync(
            new CreateTransferCommand(request.SourceAccountId, request.DestinationAccountId, request.Amount, idempotencyKey),
            ct);

        return ToHttpResult(result);
    }

    private static async Task<IResult> GetAsync(Guid id, ITransferRepository transfers, CancellationToken ct)
    {
        var transfer = await transfers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("transfer.not_found", $"Transferência {id} não encontrada.");

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
