using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Transfers.Application.Abstractions;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Persistence;

namespace Transfers.Infrastructure.Identity;

public sealed class PasswordAuthenticationService(
    TransfersDbContext db,
    IPasswordHasher<AccountCredential> hasher) : IAuthenticationService
{
    private static readonly AccountCredential Placeholder = new(Guid.Empty);
    private static readonly string PlaceholderHash = new PasswordHasher<AccountCredential>().HashPassword(Placeholder, "placeholder");

    public async Task<Account?> AuthenticateAsync(string document, string password, CancellationToken ct)
    {
        var normalized = new string(document.Where(char.IsDigit).ToArray());

        var match = await (
                from account in db.Accounts.AsNoTracking()
                join credential in db.AccountCredentials.AsNoTracking() on account.Id equals credential.AccountId
                where account.HolderDocument == normalized
                select new { Account = account, Credential = credential })
            .FirstOrDefaultAsync(ct);

        if (match is null)
        {
            hasher.VerifyHashedPassword(Placeholder, PlaceholderHash, password);
            return null;
        }

        var result = hasher.VerifyHashedPassword(match.Credential, match.Credential.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : match.Account;
    }
}
