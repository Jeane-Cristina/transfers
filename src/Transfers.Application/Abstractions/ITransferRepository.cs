using Transfers.Domain.Rules;
using Transfers.Domain.Transfers;

namespace Transfers.Application.Abstractions;

public interface ITransferRepository
{
    void Add(Transfer transfer);
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Transfer?> LockForUpdateAsync(Guid id, CancellationToken ct);
    Task<Transfer?> GetByIdempotencyKeyAsync(string key, CancellationToken ct);
    Task<HourlyUsage> GetHourlyUsageAsync(Guid sourceAccountId, DateTimeOffset since, CancellationToken ct);
    Task<(IReadOnlyList<Transfer> Items, int Total)> ListByAccountAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Transfer>> LockDueScheduledAsync(DateTimeOffset now, int batchSize, CancellationToken ct);
}
