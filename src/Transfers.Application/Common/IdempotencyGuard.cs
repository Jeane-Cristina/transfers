using Transfers.Application.Abstractions;
using Transfers.Application.Transfers;
using Transfers.Domain.Common;

namespace Transfers.Application.Common;

public sealed class IdempotencyGuard(ITransferRepository transfers)
{
    public const int MaxKeyLength = 100;

    public async Task<TransferExecution?> FindReplayAsync(string? key, string requestHash, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        if (key.Length > MaxKeyLength)
            throw new DomainException("idempotency.key_too_long", "A Idempotency-Key aceita até 100 caracteres.");

        var existing = await transfers.GetByIdempotencyKeyAsync(key, ct);
        if (existing is null)
            return null;

        if (existing.RequestHash != requestHash)
            throw new ConflictException("idempotency.key_reused", "Esta Idempotency-Key já foi usada com outro conteúdo.");

        return new TransferExecution(TransferResponse.From(existing), Replayed: true);
    }
}
