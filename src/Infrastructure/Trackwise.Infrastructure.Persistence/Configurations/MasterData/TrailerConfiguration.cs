using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class TrailerConfiguration : IEntityTypeConfiguration<Trailer>
{
    public void Configure(EntityTypeBuilder<Trailer> builder)
    {
        builder.ToTable("trailers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Filtered rather than plain unique: a soft-deleted record keeps its code, and an
        // unfiltered index would let it hold that code hostage forever.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(x => x.Vin).HasMaxLength(17);

        // Unique among live rows for the same reason as Code, but only where present: not every
        // trailer has a VIN on file, and PostgreSQL treats NULLs as distinct, so several trailers
        // without one coexist happily under this index.
        builder.HasIndex(x => x.Vin)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND vin IS NOT NULL");

        builder.Property(x => x.LicensePlate).HasMaxLength(20);
        builder.Property(x => x.LicensePlateState).HasMaxLength(10);
        builder.Property(x => x.Make).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.MaxLoadCapacityKg).HasPrecision(10, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which
        // tells the steward what is scoped to it. A cascade would silently take the trailers with it.
        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);

        builder.HasOne<Trailer>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(x => x.UpdatedAt);
    }
}
