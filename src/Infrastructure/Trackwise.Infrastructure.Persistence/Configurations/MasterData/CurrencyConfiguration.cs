using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(x => x.Id);

        // Three, not thirty. ISO 4217 alpha-3 is the whole alphabet this column has, and the
        // narrower type is what stops a steward pasting a currency name into it.
        builder.Property(x => x.Code)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        builder.Property(x => x.NumericCode).HasMaxLength(3);

        // Filtered for ContainerType.IsoSizeTypeCode's reason: unique among live rows where present,
        // and plenty of records will carry no numeric code at all.
        builder.HasIndex(x => x.NumericCode)
            .IsUnique()
            .HasFilter("deleted_at IS NULL AND numeric_code IS NOT NULL");

        builder.Property(x => x.Symbol).HasMaxLength(10);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.MinorUnits).IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        builder.HasOne<Currency>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UpdatedAt);
    }
}
