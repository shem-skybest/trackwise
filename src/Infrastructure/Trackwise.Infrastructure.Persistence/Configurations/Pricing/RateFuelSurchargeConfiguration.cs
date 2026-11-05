using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.Pricing;

internal sealed class RateFuelSurchargeConfiguration : IEntityTypeConfiguration<RateFuelSurcharge>
{
    public void Configure(EntityTypeBuilder<RateFuelSurcharge> builder)
    {
        builder.ToTable("rate_fuel_surcharges");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DieselPrice).HasPrecision(9, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.RateAgreementLineId, x.DieselPrice }).IsUnique();
    }
}
