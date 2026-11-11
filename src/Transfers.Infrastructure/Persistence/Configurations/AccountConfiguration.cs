using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;

namespace Transfers.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", table =>
        {
            table.HasCheckConstraint("ck_accounts_balance_within_overdraft", "balance >= -overdraft_limit");
            table.HasCheckConstraint("ck_accounts_overdraft_non_negative", "overdraft_limit >= 0");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.HolderName).HasMaxLength(150).IsRequired();
        builder.Property(a => a.HolderDocument).HasMaxLength(14).IsRequired();
        builder.HasIndex(a => a.HolderDocument).IsUnique();

        builder.Property(a => a.Balance).HasPrecision(18, 2);
        builder.Property(a => a.OverdraftLimit).HasPrecision(18, 2);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        builder.ComplexProperty(a => a.Limits, limits =>
        {
            limits.Property(l => l.DayMaxAmount).HasPrecision(18, 2);
            limits.Property(l => l.NightMaxAmount).HasPrecision(18, 2);
        });

        builder.Ignore(a => a.DomainEvents);
    }
}
