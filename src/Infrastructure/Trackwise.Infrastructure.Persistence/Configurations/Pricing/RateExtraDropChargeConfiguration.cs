using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.Pricing;

internal sealed class RateExtraDropChargeConfiguration : IEntityTypeConfiguration<RateExtraDropCharge>
{
    public void Configure(EntityTypeBuilder<RateExtraDropCharge> builder)
    {
        builder.ToTable("rate_extra_drop_charges");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.RateAgreementId, x.Tonnage }).IsUnique();
    }
}
