using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository(TransfersDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken ct)
        => await db.Accounts.AsNoTracking().OrderBy(a => a.HolderName).ToListAsync(ct);

    public async Task<IReadOnlyList<Account>> LockForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var idArray = ids.Distinct().ToArray();

        await db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM accounts WHERE id = ANY({idArray}) ORDER BY id FOR UPDATE", ct);

        return await db.Accounts
            .Where(a => idArray.Contains(a.Id))
            .ToListAsync(ct);
    }
}
