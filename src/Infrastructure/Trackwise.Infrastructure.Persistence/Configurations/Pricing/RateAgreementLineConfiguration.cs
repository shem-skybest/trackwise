using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.Pricing;

internal sealed class RateAgreementLineConfiguration : IEntityTypeConfiguration<RateAgreementLine>
{
    public void Configure(EntityTypeBuilder<RateAgreementLine> builder)
    {
        builder.ToTable("rate_agreement_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Province).HasMaxLength(100);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.FuelPercent).HasPrecision(9, 4);

        builder.HasMany(x => x.FuelSurcharges)
            .WithOne()
            .HasForeignKey(x => x.RateAgreementLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RateAgreementId, x.SortOrder });
    }
}
