using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ShortName).HasMaxLength(20);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.TaxId).HasMaxLength(50);

        builder.Property(x => x.IsActive).IsRequired();

        // Filtered rather than plain unique: a code is reusable only once its holder is gone for
        // good, and soft-deleted rows keep theirs. Matches how the soft-delete query filter reads.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.HasIndex(x => x.IsActive);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(x => x.UpdatedAt);
    }
}
