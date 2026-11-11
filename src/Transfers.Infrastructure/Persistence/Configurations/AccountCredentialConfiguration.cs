using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;
using Transfers.Infrastructure.Identity;

namespace Transfers.Infrastructure.Persistence.Configurations;

public sealed class AccountCredentialConfiguration : IEntityTypeConfiguration<AccountCredential>
{
    public void Configure(EntityTypeBuilder<AccountCredential> builder)
    {
        builder.ToTable("account_credentials");
        builder.HasKey(c => c.AccountId);
        builder.Property(c => c.PasswordHash).HasMaxLength(200).IsRequired();
        builder.HasOne<Account>().WithOne().HasForeignKey<AccountCredential>(c => c.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
