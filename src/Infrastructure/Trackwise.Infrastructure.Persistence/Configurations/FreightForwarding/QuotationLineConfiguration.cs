using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable("quotation_lines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.FxRate)
            .HasPrecision(18, 6);

        builder.Property(x => x.MarkupType)
            .IsRequired();

        builder.Property(x => x.MarkupValue)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Quotation)
            .WithMany(q => q.Lines)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.Category)
            .IsRequired();

        builder.Property(x => x.QuotationCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasOne(x => x.TariffRate)
            .WithMany()
            .HasForeignKey(x => x.TariffRateId)
            .OnDelete(DeleteBehavior.Restrict); // a priced line must not silently lose its rate

        // Indexes
        builder.HasIndex(x => x.QuotationId);
        builder.HasIndex(x => x.TariffRateId);
    }
}
