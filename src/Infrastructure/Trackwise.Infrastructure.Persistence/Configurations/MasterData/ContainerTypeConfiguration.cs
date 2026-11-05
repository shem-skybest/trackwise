using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class ContainerTypeConfiguration : IEntityTypeConfiguration<ContainerType>
{
    public void Configure(EntityTypeBuilder<ContainerType> builder)
    {
        builder.ToTable("container_types");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(x => x.IsoSizeTypeCode).HasMaxLength(10);

        // Filtered, not a plain unique index. An ISO size-type code is unique among live rows
        // WHERE PRESENT: the road tanker and bulk rows genuinely have none, and PostgreSQL treating
        // NULLs as distinct is exactly what makes the filtered form work where a unique constraint
        // would not. Vessel.ImoNumber carries the same shape for the same reason.
        builder.HasIndex(x => x.IsoSizeTypeCode)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND iso_size_type_code IS NOT NULL");

        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.IsHighCube).IsRequired();

        // Two decimals is enough for every factor that exists - 1, 2 and 2.25 - and pinning it
        // stops a 45' being stored as 2.2499999.
        builder.Property(x => x.TeuFactor).HasPrecision(5, 2);

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        builder.HasOne<ContainerType>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UpdatedAt);
    }
}
