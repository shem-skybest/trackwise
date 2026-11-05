using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TruckEntityConfiguration : IEntityTypeConfiguration<Truck>
{
    public void Configure(EntityTypeBuilder<Truck> builder)
    {
        builder.ToTable("trucks");

        builder.Property(t => t.Number)
            .HasMaxLength(30)
            .IsRequired();

        // Filtered rather than plain unique, now that a truck can be soft-deleted: a deleted record
        // keeps its number, and an unfiltered index would let it hold that number hostage forever.
        builder.HasIndex(t => t.Number)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Nullable and unique only among live rows where present - most legacy trucks have no VIN
        // on file, and a soft-deleted truck must not hold a VIN hostage from the one that replaced it.
        builder.HasIndex(t => t.Vin)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND vin IS NOT NULL");

        builder.Property(t => t.AssetCode).HasMaxLength(20);
        builder.Property(t => t.ConductionSticker).HasMaxLength(20);
        builder.Property(t => t.EngineNumber).HasMaxLength(30);
        builder.Property(t => t.Supplier).HasMaxLength(100);

        builder.Property(t => t.IsActive).IsRequired();
        builder.HasIndex(t => t.IsActive);

        builder.ComplexProperty(i => i.AdrEquipment, adr =>
        {
            adr.Property(a => a.OrangePlateNumber).HasMaxLength(8);
        });

        builder.HasOne(i => i.MainDriver)
            .WithMany()
            .HasForeignKey(i => i.MainDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.SecondaryDriver)
            .WithMany()
            .HasForeignKey(i => i.SecondaryDriverId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Loads)
            .WithOne(i => i.AssignedTruck)
            .HasForeignKey(i => i.AssignedTruckId)
            .OnDelete(DeleteBehavior.SetNull);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which
        // tells the steward what is scoped to it. A cascade would silently take the trucks with it.
        builder.HasOne(t => t.Branch)
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.BranchId);

        builder.HasOne<Truck>()
            .WithMany()
            .HasForeignKey(t => t.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(t => t.UpdatedAt);
    }
}
