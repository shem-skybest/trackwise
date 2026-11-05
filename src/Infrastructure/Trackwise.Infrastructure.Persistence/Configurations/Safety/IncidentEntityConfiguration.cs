using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class IncidentEntityConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");

        builder.Property(i => i.CaseNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(i => i.CaseNumber).IsUnique();
        builder.HasIndex(i => i.OccurredAt);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.TripId);
        builder.HasIndex(i => new { i.DriverId, i.OccurredAt });
        builder.HasIndex(i => new { i.TruckId, i.OccurredAt });

        // SetNull throughout: the incident is the record of what happened and outlives a trip, truck,
        // client or crew member that is later deleted or merged away.
        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Truck>()
            .WithMany()
            .HasForeignKey(i => i.TruckId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(i => i.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Helper>()
            .WithMany()
            .HasForeignKey(i => i.HelperId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Helper>()
            .WithMany()
            .HasForeignKey(i => i.SecondHelperId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Documents)
            .WithOne(d => d.Incident)
            .HasForeignKey(d => d.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Witnesses)
            .WithOne(i => i.Incident)
            .HasForeignKey(i => i.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.ThirdParties)
            .WithOne(i => i.Incident)
            .HasForeignKey(i => i.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Summary).HasMaxLength(4000);
        builder.Property(i => i.Prevention).HasMaxLength(4000);
        builder.Property(i => i.Place).HasMaxLength(500);
        builder.Property(i => i.InjuryDescription).HasMaxLength(2000);
        builder.Property(i => i.VehicleDamageDescription).HasMaxLength(2000);
        builder.Property(i => i.PoliceReportNumber).HasMaxLength(100);
        builder.Property(i => i.PoliceStation).HasMaxLength(200);
        builder.Property(i => i.BarangayBlotterNumber).HasMaxLength(100);
        builder.Property(i => i.InsuranceClaimNumber).HasMaxLength(100);
        builder.Property(i => i.CrewChargeNote).HasMaxLength(500);
        builder.Property(i => i.ReviewedBy).HasMaxLength(200);
        builder.Property(i => i.ReviewNotes).HasMaxLength(2000);
        builder.Property(i => i.CorrectiveAction).HasMaxLength(4000);
        builder.Property(i => i.ResolvedBy).HasMaxLength(200);
        builder.Property(i => i.ReopenReason).HasMaxLength(500);
        builder.Property(i => i.ClosedBy).HasMaxLength(200);
        builder.Property(i => i.EstimatedDamageCost).HasPrecision(18, 2);
        builder.Property(i => i.ThirdPartyCompensation).HasPrecision(18, 2);
        builder.Property(i => i.CrewChargeAmount).HasPrecision(18, 2);
    }
}
