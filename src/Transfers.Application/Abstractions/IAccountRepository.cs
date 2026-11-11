using Transfers.Domain.Accounts;

namespace Transfers.Application.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken ct);
    Task<IReadOnlyList<Account>> LockForUpdateAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}
