using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trackwise.Domain.Entities;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(x => x.Id);

        // ── Header ───────────────────────────────────────────────────────────
        // BookingNumber is derived from the quotation reference (BK-2026-00042),
        // so it is a plain string, NOT a database identity column.
        builder.Property(x => x.BookingNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.CustomerId).IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.CustomerReference).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        // ── Quotation link ───────────────────────────────────────────────────
        builder.Property(x => x.QuotationReference)
            .HasMaxLength(50)
            .IsRequired();

        // Must reference the Quotation navigation. Configuring HasOne<Quotation>() without it makes EF
        // create a SECOND relationship for the nav, with a shadow FK (quotation_id1) that is never set.
        builder.HasOne(x => x.Quotation)
            .WithMany()
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Restrict); // a Booking must not silently lose its originating Quotation

        // One Booking per Quotation - conversion is a one-way, one-time transition.
        builder.HasIndex(x => x.QuotationId)
            .IsUnique();

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Logistics context (snapshot of the quotation) ────────────────────
        builder.Property(x => x.Mode).IsRequired();
        builder.Property(x => x.FreightMode).IsRequired();
        builder.Property(x => x.ShipmentType).IsRequired();
        builder.Property(x => x.Incoterm).IsRequired();

        builder.Property(x => x.DeclaredCargoValuePhp).HasPrecision(18, 2);

        // Value objects stored as JSON. Mirror whatever QuotationConfiguration does for the
        // same types so both aggregates serialize identically.
        builder.OwnsOne(x => x.Lane, lane =>
        {
            lane.ToJson("lane");
            lane.OwnsMany(l => l.Stops);
        });

        builder.OwnsOne(x => x.CargoSpec, cargo =>
        {
            cargo.ToJson("cargo_spec");
            cargo.OwnsOne(c => c.DeclaredCargoValue);
        });

        builder.OwnsOne(x => x.TemperatureSpec, temp => temp.ToJson("temperature_spec"));

        builder.Ignore(x => x.IsTemperatureControlled);
        builder.Ignore(x => x.RequestedPickupStart);   // computed from Lane
        builder.Ignore(x => x.RequestedDeliveryEnd);   // computed from Lane

        // ── Billing terms ────────────────────────────────────────────────────
        builder.Property(x => x.PaymentTermsDays).IsRequired();
        builder.Property(x => x.DepositRequiredPercent).HasPrecision(5, 2);

        // ── Credit check ─────────────────────────────────────────────────────
        builder.Property(x => x.CreditCheckStatus).IsRequired();
        builder.Property(x => x.CreditCheckedBy).HasMaxLength(450);
        builder.Property(x => x.CreditCheckNotes).HasMaxLength(1000);

        // ── Lifecycle ────────────────────────────────────────────────────────
        builder.Property(x => x.ConfirmedBy).HasMaxLength(450);
        builder.Property(x => x.CancelledBy).HasMaxLength(450);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);

        builder.Property(x => x.CreatedAt).IsRequired();

        // ── Children ─────────────────────────────────────────────────────────
        builder.HasMany(x => x.Charges)
            .WithOne(c => c.Booking)
            .HasForeignKey(c => c.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Documents)
            .WithOne()
            .HasForeignKey(d => d.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Charges).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Documents).UsePropertyAccessMode(PropertyAccessMode.Field);

        // ── Query indexes ────────────────────────────────────────────────────
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreditCheckStatus);
    }
}

internal sealed class BookingChargeLineConfiguration : IEntityTypeConfiguration<BookingChargeLine>
{
    public void Configure(EntityTypeBuilder<BookingChargeLine> builder)
    {
        builder.ToTable("booking_charge_lines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.ServiceType).IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.BuyingAmount).HasPrecision(18, 2);
        builder.Property(x => x.SellingAmount).HasPrecision(18, 2);

        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.BillingStatus).IsRequired();

        // QuotationLineId / TariffRateId are traceability snapshots, deliberately without FKs:
        // the booking must survive later changes or deletion of tariffs. InvoiceId is a
        // cross-module reference to Billing, so it has no FK either.
        builder.HasIndex(x => new { x.BookingId, x.BillingStatus });
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.QuotationLineId);
    }
}

internal sealed class BookingDocumentConfiguration : IEntityTypeConfiguration<BookingDocument>
{
    public void Configure(EntityTypeBuilder<BookingDocument> builder)
    {
        builder.ToTable("booking_documents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingId).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.Property(x => x.Reference).HasMaxLength(100);
        builder.Property(x => x.ReviewedBy).HasMaxLength(450);
        builder.Property(x => x.ReviewNotes).HasMaxLength(1000);

        builder.Ignore(x => x.IsSatisfied);   // computed from Status + ExpiresAt

        builder.HasIndex(x => new { x.BookingId, x.Type });
        builder.HasIndex(x => x.ExpiresAt);
    }
}
