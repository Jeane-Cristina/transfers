using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Rules;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Repositories;

public sealed class TransferRepository(TransfersDbContext db) : ITransferRepository
{
    public void Add(Transfer transfer) => db.Transfers.Add(transfer);

    public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Transfers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Transfer?> LockForUpdateAsync(Guid id, CancellationToken ct)
    {
        var rows = await db.Transfers
            .FromSql($"SELECT * FROM transfers WHERE id = {id} FOR UPDATE")
            .ToListAsync(ct);

        return rows.FirstOrDefault();
    }

    public Task<Transfer?> GetByIdempotencyKeyAsync(string key, CancellationToken ct)
        => db.Transfers.FirstOrDefaultAsync(t => t.IdempotencyKey == key, ct);

    public async Task<HourlyUsage> GetHourlyUsageAsync(Guid sourceAccountId, DateTimeOffset since, CancellationToken ct)
    {
        var window = db.Transfers.Where(t =>
            t.SourceAccountId == sourceAccountId &&
            t.ProcessedAt >= since &&
            (t.Status == TransferStatus.Completed || t.Status == TransferStatus.Failed));

        var attempts = await window.CountAsync(ct);
        var completedAmount = await window
            .Where(t => t.Status == TransferStatus.Completed)
            .SumAsync(t => t.Amount, ct);

        return new HourlyUsage(attempts, completedAmount);
    }

    public async Task<(IReadOnlyList<Transfer> Items, int Total)> ListByAccountAsync(
        Guid accountId, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Transfers.AsNoTracking()
            .Where(t => t.SourceAccountId == accountId || t.DestinationAccountId == accountId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Transfer>> LockDueScheduledAsync(
        DateTimeOffset now, int batchSize, CancellationToken ct)
        => await db.Transfers
            .FromSql($"""
                SELECT * FROM transfers
                WHERE status = 'Scheduled' AND scheduled_for <= {now}
                ORDER BY scheduled_for
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);
}
