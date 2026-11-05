using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class BillingBatchEntityConfiguration : IEntityTypeConfiguration<BillingBatch>
{
    public void Configure(EntityTypeBuilder<BillingBatch> builder)
    {
        builder.ToTable("billing_batches");

        builder.Property(b => b.ReferenceNumber).HasMaxLength(32).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(1000);
        builder.Property(b => b.VatableTotal).HasPrecision(18, 2);
        builder.Property(b => b.NonVatTotal).HasPrecision(18, 2);
        builder.Property(b => b.LockedBy).HasMaxLength(200);
        builder.Property(b => b.UnlockReason).HasMaxLength(500);
        builder.Property(b => b.UnlockedBy).HasMaxLength(200);
        builder.Property(b => b.ForwardedBy).HasMaxLength(200);
        builder.Property(b => b.InvoiceNumber).HasMaxLength(100);
        builder.Property(b => b.BilledBy).HasMaxLength(200);
        builder.Property(b => b.CancelReason).HasMaxLength(500);
        builder.Property(b => b.CancelledBy).HasMaxLength(200);
        builder.Ignore(b => b.Total);
        builder.Ignore(b => b.Period);

        builder.HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Trips)
            .WithOne()
            .HasForeignKey(t => t.BillingBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Charges)
            .WithOne()
            .HasForeignKey(c => c.BillingBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.ReferenceNumber).IsUnique();
        builder.HasIndex(b => new { b.CustomerId, b.PeriodStart });
        builder.HasIndex(b => b.Status);
    }
}
