using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TripEntityConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("trips");

        builder.Property(i => i.Number)
            .UseIdentityAlwaysColumn()
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(i => i.Number)
            .IsUnique();

        builder.HasMany(i => i.Stops)
            .WithOne(i => i.Trip)
            .HasForeignKey(i => i.TripId)
            .OnDelete(DeleteBehavior.Cascade);

        // SetNull, not cascade/restrict: a merged/deleted Helper must not take a historical trip
        // record down with it, same reasoning Truck's MainDriver/SecondaryDriver FKs use.
        builder.HasOne(i => i.Helper)
            .WithMany()
            .HasForeignKey(i => i.HelperId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.SecondHelper)
            .WithMany()
            .HasForeignKey(i => i.SecondHelperId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Driver)
            .WithMany()
            .HasForeignKey(i => i.DriverId)
            .OnDelete(DeleteBehavior.SetNull);

        // The trip keeps its own copy of the price, so losing the agreement only loses the link.
        builder.HasOne<RateAgreement>()
            .WithMany()
            .HasForeignKey(i => i.RateAgreementId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(i => i.RateLineLabel).HasMaxLength(200);
        builder.Property(i => i.RateFuelBasis).HasMaxLength(200);
        builder.Property(i => i.RateBaseAmount).HasPrecision(18, 2);
        builder.Property(i => i.RateFuelAmount).HasPrecision(18, 2);
        builder.Property(i => i.RateExtraDropAmount).HasPrecision(18, 2);
        builder.Ignore(i => i.RateTotal);
    }
}
