using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.Pricing;

internal sealed class DieselPriceConfiguration : IEntityTypeConfiguration<DieselPrice>
{
    public void Configure(EntityTypeBuilder<DieselPrice> builder)
    {
        builder.ToTable("diesel_prices");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PricePerLitre).HasPrecision(9, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);

        // One price per day: two entries on one date would make "the price in force" ambiguous.
        builder.HasIndex(x => x.EffectiveFrom).IsUnique();
    }
}
