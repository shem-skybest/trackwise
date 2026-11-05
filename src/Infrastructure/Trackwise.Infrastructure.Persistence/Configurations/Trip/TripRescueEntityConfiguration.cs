using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TripRescueEntityConfiguration : IEntityTypeConfiguration<TripRescue>
{
    public void Configure(EntityTypeBuilder<TripRescue> builder)
    {
        builder.ToTable("trip_rescues");

        builder.HasOne<Trip>()
            .WithMany(i => i.Rescues)
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<TripStop>()
            .WithOne()
            .HasForeignKey(i => i.RescueId)
            .OnDelete(DeleteBehavior.SetNull);

        // Trucks are soft-deleted, so a rescue never loses its truck; crew are SetNull like the trip's own.
        builder.HasOne<Truck>()
            .WithMany()
            .HasForeignKey(i => i.TruckId)
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.HasOne<Incident>()
            .WithMany()
            .HasForeignKey(i => i.IncidentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.TruckId);

        builder.Property(i => i.Place).HasMaxLength(500);
        builder.Property(i => i.Reason).HasMaxLength(500);
        builder.Property(i => i.RecordedBy).HasMaxLength(200);
    }
}
