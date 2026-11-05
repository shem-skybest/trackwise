using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class VehiclePermitEntityConfiguration : IEntityTypeConfiguration<VehiclePermit>
{
    public void Configure(EntityTypeBuilder<VehiclePermit> builder)
    {
        builder.ToTable("vehicle_permits", t => t.HasCheckConstraint(
            "ck_vehicle_permits_one_vehicle",
            "(truck_id IS NULL) <> (trailer_id IS NULL)"));

        builder.Property(i => i.Number).HasMaxLength(64);
        builder.Property(i => i.Issuer).HasMaxLength(100);
        builder.Property(i => i.Notes).HasMaxLength(500);
        builder.Property(i => i.Cost).HasPrecision(18, 2);

        builder.HasOne(i => i.Truck)
            .WithMany()
            .HasForeignKey(i => i.TruckId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Trailer)
            .WithMany()
            .HasForeignKey(i => i.TrailerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TruckId, i.Type });
        builder.HasIndex(i => new { i.TrailerId, i.Type });
        builder.HasIndex(i => i.ExpiresOn);
    }
}
