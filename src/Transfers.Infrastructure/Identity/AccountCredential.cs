namespace Transfers.Infrastructure.Identity;

public sealed class AccountCredential
{
    public Guid AccountId { get; private set; }
    public string PasswordHash { get; private set; } = default!;

    private AccountCredential() { }

    public AccountCredential(Guid accountId) => AccountId = accountId;

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
