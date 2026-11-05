using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class BillingBatchChargeEntityConfiguration : IEntityTypeConfiguration<BillingBatchCharge>
{
    public void Configure(EntityTypeBuilder<BillingBatchCharge> builder)
    {
        builder.ToTable("billing_batch_charges");

        builder.Property(c => c.Description).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(c => c.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<CashFundExpense>()
            .WithMany()
            .HasForeignKey(c => c.CashFundExpenseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.BillingBatchId);
        builder.HasIndex(c => c.TripId);
        builder.HasIndex(c => c.CashFundExpenseId)
            .IsUnique()
            .HasFilter("cash_fund_expense_id IS NOT NULL");
    }
}
