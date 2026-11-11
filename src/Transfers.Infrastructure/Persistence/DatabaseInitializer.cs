using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TransfersDbContext>();

        await db.Database.MigrateAsync(ct);

        if (await db.Accounts.AnyAsync(ct))
            return;

        db.Accounts.AddRange(
            Account.Open("Ana Souza", "11111111111", initialBalance: 500m, overdraftLimit: 1_000m),
            Account.Open("Bruno Lima", "22222222222", initialBalance: -800m, overdraftLimit: 1_000m),
            Account.Open("Carla Dias", "33333333333", initialBalance: 20_000m, overdraftLimit: 0m),
            Account.Open("Diego Rocha", "44444444444", initialBalance: 100m, overdraftLimit: 0m,
                status: AccountStatus.Blocked));

        await db.SaveChangesAsync(ct);
    }
}
