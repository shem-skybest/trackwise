using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class BillingBatchTripEntityConfiguration : IEntityTypeConfiguration<BillingBatchTrip>
{
    public void Configure(EntityTypeBuilder<BillingBatchTrip> builder)
    {
        builder.ToTable("billing_batch_trips");

        builder.Property(t => t.TruckNumber).HasMaxLength(64);
        builder.Property(t => t.BookingReferences).HasMaxLength(500);
        builder.Property(t => t.RateLineLabel).HasMaxLength(200);
        builder.Property(t => t.BaseAmount).HasPrecision(18, 2);
        builder.Property(t => t.FuelAmount).HasPrecision(18, 2);
        builder.Property(t => t.ExtraDropAmount).HasPrecision(18, 2);
        builder.Property(t => t.Amount).HasPrecision(18, 2);
        builder.Ignore(t => t.Problem);

        builder.HasOne(t => t.Trip)
            .WithMany()
            .HasForeignKey(t => t.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        // A trip is billed once: cancelling a batch deletes its lines, which frees the trip.
        builder.HasIndex(t => t.TripId).IsUnique();
        builder.HasIndex(t => t.BillingBatchId);
    }
}
