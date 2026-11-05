using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class CrewAssignmentEntityConfiguration : IEntityTypeConfiguration<CrewAssignment>
{
    public void Configure(EntityTypeBuilder<CrewAssignment> builder)
    {
        builder.ToTable("crew_assignments");

        builder.Property(i => i.Notes).HasMaxLength(500);

        builder.HasOne(i => i.Truck)
            .WithMany()
            .HasForeignKey(i => i.TruckId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Driver)
            .WithMany()
            .HasForeignKey(i => i.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Helper)
            .WithMany()
            .HasForeignKey(i => i.HelperId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.SecondHelper)
            .WithMany()
            .HasForeignKey(i => i.SecondHelperId)
            .OnDelete(DeleteBehavior.SetNull);

        // One plan row per truck per day. It also keeps the table out of a truck merge's
        // MergeReferences: repointing truck_id could collide with the survivor's own row that day.
        builder.HasIndex(i => new { i.Date, i.TruckId }).IsUnique();
    }
}
