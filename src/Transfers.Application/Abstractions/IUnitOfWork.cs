namespace Transfers.Application.Abstractions;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken ct);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct);
}
