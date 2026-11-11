using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Transfers.Domain.Accounts;
using Transfers.Domain.Transfers;

namespace Transfers.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers", table =>
        {
            table.HasCheckConstraint("ck_transfers_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_transfers_distinct_accounts", "source_account_id <> destination_account_id");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.FailureReason).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.IdempotencyKey).HasMaxLength(100);
        builder.Property(t => t.RequestHash).HasMaxLength(64);

        builder.HasOne<Account>().WithMany().HasForeignKey(t => t.SourceAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(t => t.DestinationAccountId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.SourceAccountId, t.ProcessedAt });
        builder.HasIndex(t => t.ScheduledFor).HasFilter("status = 'Scheduled'");
        builder.HasIndex(t => t.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");

        builder.Ignore(t => t.DomainEvents);
    }
}
