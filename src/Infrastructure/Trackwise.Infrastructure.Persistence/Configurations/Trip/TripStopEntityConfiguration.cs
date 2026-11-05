using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TripStopEntityConfiguration : IEntityTypeConfiguration<TripStop>
{
    public void Configure(EntityTypeBuilder<TripStop> builder)
    {
        builder.ToTable("trip_stops");

        builder.HasOne(i => i.Trip)
            .WithMany(i => i.Stops)
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.Load)
            .WithMany(l => l.TripStops)
            .HasForeignKey(ts => ts.LoadId)
            .OnDelete(DeleteBehavior.Restrict);

        // Realistic reefer range (frozen negatives through ambient); explicit precision avoids
        // EF's "decimal column with no precision/scale" warning defaulting it to (18,2) silently.
        builder.Property(ts => ts.TemperatureAtArrival).HasPrecision(5, 2);
        builder.Property(ts => ts.TemperatureAtDeparture).HasPrecision(5, 2);

        builder.Property(ts => ts.ArrivalRecordedBy).HasMaxLength(200);
        builder.Property(ts => ts.DepartureRecordedBy).HasMaxLength(200);
    }
}
