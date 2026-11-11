using Transfers.Domain.Accounts;

namespace Transfers.Application.Abstractions;

public interface IAuthenticationService
{
    Task<Account?> AuthenticateAsync(string document, string password, CancellationToken ct);
}
