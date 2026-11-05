using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TerminalEntityConfiguration : IEntityTypeConfiguration<Terminal>
{
    public void Configure(EntityTypeBuilder<Terminal> builder)
    {
        builder.ToTable("terminals");

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        // Wider than a UN/LOCODE's five characters: the legacy TBL_PORT rows this table absorbs
        // include inland depots and border crossings that never had one, and truncating their code
        // would strand the legacy keys that join on it.
        builder.Property(t => t.Code)
            .HasMaxLength(30)
            .IsRequired();

        // Filtered rather than plain unique, now that a terminal can be soft-deleted: a deleted
        // record keeps its code, and an unfiltered index would let it hold that code hostage
        // forever. Matches how the soft-delete query filter reads.
        builder.HasIndex(t => t.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(t => t.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(t => t.Notes).HasMaxLength(2000);

        builder.Property(t => t.IsActive).IsRequired();
        builder.HasIndex(t => t.IsActive);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which
        // tells the steward what is scoped to it. A cascade would silently take the ports with it.
        builder.HasOne(t => t.Branch)
            .WithMany()
            .HasForeignKey(t => t.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.BranchId);

        builder.HasOne<Terminal>()
            .WithMany()
            .HasForeignKey(t => t.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(t => t.UpdatedAt);
    }
}
