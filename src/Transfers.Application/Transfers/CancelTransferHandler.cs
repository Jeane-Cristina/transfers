using Transfers.Application.Abstractions;
using Transfers.Application.Common;

namespace Transfers.Application.Transfers;

public sealed class CancelTransferHandler(ITransferRepository transfers, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public Task<TransferResponse> HandleAsync(Guid transferId, CancellationToken ct)
        => unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var transfer = await transfers.LockForUpdateAsync(transferId, token)
                ?? throw new NotFoundException("transfer.not_found", $"Transferência {transferId} não encontrada.");

            transfer.Cancel(clock.GetUtcNow());
            return TransferResponse.From(transfer);
        }, ct);
}
