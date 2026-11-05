using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class TariffRateConfiguration : IEntityTypeConfiguration<TariffRate>
{
    public void Configure(EntityTypeBuilder<TariffRate> builder)
    {
        builder.ToTable("tariff_rates");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Unit)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.PercentageBase)
            .HasMaxLength(20);

        builder.Property(x => x.BreakpointJson)
            .HasColumnType("text");

        builder.Property(x => x.Minimum)
            .HasPrecision(18, 2);

        builder.Property(x => x.Maximum)
            .HasPrecision(18, 2);

        builder.Property(x => x.FreeDays)
            .IsRequired(false);

        builder.Property(x => x.RateAfterFreeDays)
            .HasPrecision(18, 2);

        builder.Property(x => x.Percentage)
            .HasPrecision(18, 2);

        // Relationships
        builder.HasOne(x => x.Tariff)
            .WithMany(t => t.Rates)
            .HasForeignKey(x => x.TariffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChargeCode)
            .WithMany(x => x.Rates)
            .HasForeignKey(x => x.ChargeCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.BreakpointMode)
            .IsRequired();

        // Index
        builder.HasIndex(x => new { x.TariffId, x.ChargeCodeId });
    }
}
