using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable("quotations");

        builder.HasKey(x => x.Id);

        // Core Identification & Tenancy
        builder.Property(x => x.CustomerId)
            .IsRequired();

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        // Operational & Trade Modes
        builder.Property(x => x.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.FreightMode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ShipmentType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Incoterm)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // Compliance & Special Handling Flags
        builder.Property(x => x.IsHazardous)
            .IsRequired();

        builder.Property(x => x.RequiresCustomsBrokerage)
            .IsRequired();

        builder.Property(x => x.RequiresSpecialApproval)
            .IsRequired();

        builder.Property(x => x.ApprovalEscalationReason)
            .HasMaxLength(500);

        // Lifecycle & Audit Timestamps
        builder.Property(x => x.ValidUntil);
        builder.Property(x => x.IssuedAt);
        builder.Property(x => x.AcceptedAt);

        builder.Property(x => x.ApprovedBy)
            .HasMaxLength(450);
        builder.Property(x => x.ApprovedAt);

        builder.Property(x => x.RejectedBy)
            .HasMaxLength(450);
        builder.Property(x => x.RejectedAt);
        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // =========================================================================
        // Value Object: Cargo Specification
        // =========================================================================
        builder.OwnsOne(x => x.CargoSpec, cargo =>
        {
            cargo.Property(c => c.Commodity)
                .HasMaxLength(200)
                .HasColumnName("Commodity");

            cargo.Property(c => c.TotalWeightKg)
                .HasPrecision(18, 2)
                .HasColumnName("TotalWeightKg");

            cargo.Property(c => c.PalletCount)
                .HasColumnName("PalletCount");

            cargo.Property(c => c.RequiresActiveRefrigeration)
                .HasColumnName("RequiresActiveRefrigeration");

            cargo.OwnsOne(c => c.DeclaredCargoValue, money =>
            {
                money.Property(m => m.Amount)
                    .HasPrecision(18, 2)
                    .HasColumnName("DeclaredCargoValueAmount");

                money.Property(m => m.Currency)
                    .HasMaxLength(3)
                    .HasColumnName("DeclaredCargoValueCurrency");
            });
        });

        // Explicitly mark the navigation as optional
        builder.Navigation(x => x.CargoSpec).IsRequired(false);

        // =========================================================================
        // Value Object: Temperature Control (Nullable Owned Type)
        // =========================================================================
        builder.OwnsOne(x => x.TemperatureSpec, temp =>
        {
            temp.Property(t => t.Zone)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasColumnName("TemperatureZone");

            temp.Property(t => t.SetpointCelsius)
                .HasPrecision(5, 2)
                .HasColumnName("TemperatureSetpointCelsius");

            temp.Property(t => t.AllowableVarianceCelsius)
                .HasPrecision(5, 2)
                .HasColumnName("TemperatureVarianceCelsius");

            temp.Property(t => t.Compliance)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasColumnName("TemperatureCompliance");

            temp.Property(t => t.RequiresContinuousTelematics)
                .HasColumnName("RequiresContinuousTelematics");
        });

        // =========================================================================
        // Value Object: Lane Transit & Stops (JSON Column)
        // =========================================================================
        builder.OwnsOne(x => x.Lane, lane =>
        {
            lane.ToJson(); // Tells EF Core to store this entire object graph as JSON

            // You don't need to configure properties inside unless you want to override defaults.
            // EF Core will automatically map the collections inside (like Stops) into the JSON document.
            lane.OwnsMany(l => l.Stops);
        });

        // =========================================================================
        // Navigation: QuotationLines
        // =========================================================================
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.Quotation)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // =========================================================================
        // Indexes
        // =========================================================================
        builder.HasIndex(x => x.ReferenceNumber);
        builder.HasIndex(x => new { x.CustomerId, x.Status });
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.DeclaredCargoValuePhp)
            .HasPrecision(18, 2);
    }
}
