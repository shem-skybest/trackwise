using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class VesselConfiguration : IEntityTypeConfiguration<Vessel>
{
    public void Configure(EntityTypeBuilder<Vessel> builder)
    {
        builder.ToTable("vessels");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Filtered rather than plain unique: a soft-deleted record keeps its code, and an
        // unfiltered index would let it hold that code hostage forever. Matches how the
        // soft-delete query filter reads.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Seven digits today, but not pinned to seven in the schema - the field is stored
        // digits-only and a width that exactly fits the current standard is the kind of constraint
        // that becomes a migration the day a registry changes.
        builder.Property(x => x.ImoNumber).HasMaxLength(20);

        // Unique among live rows for the same reason as Code, but only where present: most of the
        // domestic fleet has no IMO at all, and PostgreSQL treats NULLs as distinct, so several
        // vessels without one coexist happily under this index.
        builder.HasIndex(x => x.ImoNumber)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND imo_number IS NOT NULL");

        builder.Property(x => x.CallSign).HasMaxLength(20);
        builder.Property(x => x.FlagCountryCode).HasMaxLength(2);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which
        // tells the steward what is scoped to it. A cascade would silently take the vessels with it.
        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);

        builder.HasOne<Vessel>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(x => x.UpdatedAt);
    }
}
