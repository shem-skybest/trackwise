using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class HelperConfiguration : IEntityTypeConfiguration<Helper>
{
    public void Configure(EntityTypeBuilder<Helper> builder)
    {
        builder.ToTable("helpers");

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

        builder.Property(x => x.PhoneNumber).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // Address is a single [ComplexType] property here, so it needs no explicit ComplexProperty
        // call - that is only required when an entity carries two of them and their column names
        // would otherwise collide (see LoadEntityConfiguration's Origin/DestinationAddress).

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which
        // tells the steward what is scoped to it. A cascade would silently take the helpers with it.
        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);

        builder.HasOne<Helper>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(x => x.UpdatedAt);
    }
}
