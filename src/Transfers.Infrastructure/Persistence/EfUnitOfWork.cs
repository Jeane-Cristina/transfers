using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;

namespace Transfers.Infrastructure.Persistence;

public sealed class EfUnitOfWork(TransfersDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '5s'", ct);

        var result = await work(ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct)
        => ExecuteInTransactionAsync<bool>(async token => { await work(token); return true; }, ct);
}
