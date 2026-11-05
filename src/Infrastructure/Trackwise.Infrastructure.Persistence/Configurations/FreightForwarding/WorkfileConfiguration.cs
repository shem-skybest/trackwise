using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class WorkfileConfiguration : IEntityTypeConfiguration<Workfile>
{
    public void Configure(EntityTypeBuilder<Workfile> builder)
    {
        builder.ToTable("workfiles");

        builder.HasKey(x => x.Id);

        builder.HasDiscriminator(x => x.Type)
            .HasValue<RegularWorkfile>(WorkfileType.Regular)
            .HasValue<ReeferWorkfile>(WorkfileType.Reefer)
            .HasValue<TruckingWorkfile>(WorkfileType.Trucking);

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(x => x.ReferenceNumber).IsUnique();

        builder.Property(x => x.BillOfLadingNumber).HasMaxLength(100);
        builder.Property(x => x.VesselName).HasMaxLength(200);
        builder.Property(x => x.VoyageNumber).HasMaxLength(50);
        builder.Property(x => x.Origin).HasMaxLength(200);
        builder.Property(x => x.Destination).HasMaxLength(200);
        builder.Property(x => x.Commodity).HasMaxLength(500);
        builder.Property(x => x.TaggingCategory).HasMaxLength(100);
        builder.Property(x => x.AccountingRemarks).HasMaxLength(2000);
        builder.Property(x => x.ClosedBy).HasMaxLength(450);
        builder.Property(x => x.CancelledBy).HasMaxLength(450);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);

        builder.Property(x => x.ExchangeRate).HasPrecision(18, 6);
        builder.Property(x => x.FinalExchangeRate).HasPrecision(18, 6);

        builder.Property(x => x.GensetHourlyRate).HasPrecision(18, 2);
        builder.Property(x => x.GensetHoursUsed).HasPrecision(18, 2);
        builder.Property(x => x.GensetFlatAmount).HasPrecision(18, 2);
        builder.Property(x => x.GensetNotes).HasMaxLength(1000);

        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ReportCategory).IsRequired();

        // Restrict: a workfile must not silently lose the booking it was promoted from.
        builder.HasOne(x => x.Booking)
            .WithMany()
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        // One workfile per booking - promotion is one-way and one-time, the same rule
        // BookingConfiguration applies to QuotationId. Filtered so the many Reefer files that
        // carry no booking at all do not collide with each other on NULL.
        builder.HasIndex(x => x.BookingId)
            .IsUnique()
            .HasFilter("booking_id IS NOT NULL");

        // Restrict rather than SetNull throughout: a partner named on a live file should be
        // deactivated, not deleted out from under it.
        builder.HasOne(x => x.ShippingLine).WithMany()
            .HasForeignKey(x => x.ShippingLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CoLoader).WithMany()
            .HasForeignKey(x => x.CoLoaderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Forwarder).WithMany()
            .HasForeignKey(x => x.ForwarderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Trucker).WithMany()
            .HasForeignKey(x => x.TruckerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Broker).WithMany()
            .HasForeignKey(x => x.BrokerId).OnDelete(DeleteBehavior.Restrict);

        // Restrict, same reasoning as the counterparties above: a genset assigned to a live file
        // should be retired, not deleted out from under it.
        builder.HasOne(x => x.Genset).WithMany()
            .HasForeignKey(x => x.GensetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => x.BillOfLadingNumber);
    }

    #region Derived Types Configuration

    public sealed class RegularWorkfileConfiguration : IEntityTypeConfiguration<RegularWorkfile>
    {
        public void Configure(EntityTypeBuilder<RegularWorkfile> builder)
        {
            builder.Property(x => x.Direction);
        }
    }

    public sealed class ReeferWorkfileConfiguration : IEntityTypeConfiguration<ReeferWorkfile>
    {
        public void Configure(EntityTypeBuilder<ReeferWorkfile> builder)
        {
            // No subclass-specific scalars; drops and delivery locations are separate entities.
        }
    }

    public sealed class TruckingWorkfileConfiguration : IEntityTypeConfiguration<TruckingWorkfile>
    {
        public void Configure(EntityTypeBuilder<TruckingWorkfile> builder)
        {
            builder.Property(x => x.Variant);
            builder.Property(x => x.LegRole);

            // Restrict: a forwarding file with legs keeps them - its bill covers their work.
            builder.HasOne<Workfile>()
                .WithMany()
                .HasForeignKey(x => x.ParentWorkfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ParentWorkfileId);
        }
    }

    #endregion
}
