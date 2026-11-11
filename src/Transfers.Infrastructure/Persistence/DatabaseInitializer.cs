using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Identity;

namespace Transfers.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public const string DemoPassword = "Senha@123";

    private static readonly string[] DemoDocuments = ["11111111111", "22222222222", "33333333333", "44444444444"];

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AccountCredential>>();

        await db.Database.MigrateAsync(ct);

        if (!await db.Accounts.AnyAsync(ct))
        {
            db.Accounts.AddRange(
                Account.Open("Ana Souza", "11111111111", initialBalance: 500m, overdraftLimit: 1_000m),
                Account.Open("Bruno Lima", "22222222222", initialBalance: -800m, overdraftLimit: 1_000m),
                Account.Open("Carla Dias", "33333333333", initialBalance: 20_000m, overdraftLimit: 0m),
                Account.Open("Diego Rocha", "44444444444", initialBalance: 100m, overdraftLimit: 0m,
                    status: AccountStatus.Blocked));

            await db.SaveChangesAsync(ct);
        }

        var withoutCredential = await db.Accounts
            .Where(a => DemoDocuments.Contains(a.HolderDocument)
                && !db.AccountCredentials.Any(c => c.AccountId == a.Id))
            .Select(a => a.Id)
            .ToListAsync(ct);

        foreach (var accountId in withoutCredential)
        {
            var credential = new AccountCredential(accountId);
            credential.SetPasswordHash(hasher.HashPassword(credential, DemoPassword));
            db.AccountCredentials.Add(credential);
        }

        if (withoutCredential.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
