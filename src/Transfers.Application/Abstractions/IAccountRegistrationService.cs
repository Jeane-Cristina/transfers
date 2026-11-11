using Transfers.Domain.Accounts;

namespace Transfers.Application.Abstractions;

public interface IAccountRegistrationService
{
    Task<Account> RegisterAsync(string holderName, string document, string password, CancellationToken ct);
}
