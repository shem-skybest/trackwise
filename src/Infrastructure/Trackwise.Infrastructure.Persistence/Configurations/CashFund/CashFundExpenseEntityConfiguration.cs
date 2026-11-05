using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class CashFundExpenseEntityConfiguration : IEntityTypeConfiguration<CashFundExpense>
{
    public void Configure(EntityTypeBuilder<CashFundExpense> builder)
    {
        builder.ToTable("cash_fund_expenses");

        builder.Property(i => i.Description).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.ReceiptNumber).HasMaxLength(64);
        builder.Property(i => i.ReviewedBy).HasMaxLength(200);
        builder.Property(i => i.DisallowedReason).HasMaxLength(500);

        builder.HasOne(i => i.Truck)
            .WithMany()
            .HasForeignKey(i => i.TruckId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Trip)
            .WithMany()
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.SpentBy)
            .WithMany()
            .HasForeignKey(i => i.SpentById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => new { i.CashFundId, i.SpentOn });
        builder.HasIndex(i => i.TripId);
        builder.HasIndex(i => i.TruckId);
    }
}
