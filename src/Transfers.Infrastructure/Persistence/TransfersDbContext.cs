using Microsoft.EntityFrameworkCore;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;
using Transfers.Infrastructure.Identity;
using Transfers.Infrastructure.Outbox;

namespace Transfers.Infrastructure.Persistence;

public sealed class TransfersDbContext(DbContextOptions<TransfersDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AccountCredential> AccountCredentials => Set<AccountCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransfersDbContext).Assembly);
}
