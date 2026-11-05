using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TripCorrectionEntityConfiguration : IEntityTypeConfiguration<TripCorrection>
{
    public void Configure(EntityTypeBuilder<TripCorrection> builder)
    {
        builder.ToTable("trip_corrections");

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // No foreign key on the stop: the history outlives a stop that is later removed from the trip.
        builder.HasIndex(i => i.StopId);
        builder.HasIndex(i => new { i.TripId, i.CorrectedAt });

        builder.Property(i => i.OldValue).HasMaxLength(500);
        builder.Property(i => i.NewValue).HasMaxLength(500);
        builder.Property(i => i.Reason).HasMaxLength(500);
        builder.Property(i => i.CorrectedBy).HasMaxLength(200);
    }
}
