using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.Pricing;

internal sealed class RateAgreementConfiguration : IEntityTypeConfiguration<RateAgreement>
{
    public void Configure(EntityTypeBuilder<RateAgreement> builder)
    {
        builder.ToTable("rate_agreements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);
        builder.Property(x => x.FoulTripPercent).HasPrecision(5, 2);
        builder.Property(x => x.BackloadPercent).HasPrecision(5, 2);
        builder.Property(x => x.DemurrageFreeHours).HasPrecision(5, 2);
        builder.Property(x => x.DemurrageHourlyRate).HasPrecision(18, 2);
        builder.Property(x => x.OtherLocationCharge).HasPrecision(18, 2);

        // Cascade to match the customer's loads and invoices, as its delivery locations do.
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RateAgreement>()
            .WithMany()
            .HasForeignKey(x => x.SupersedesId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.ExtraDropCharges)
            .WithOne()
            .HasForeignKey(x => x.RateAgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.RateAgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CustomerId, x.Status, x.EffectiveFrom });
    }
}
