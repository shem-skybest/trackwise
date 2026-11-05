using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class LoadBoardListingEntityConfiguration : IEntityTypeConfiguration<LoadBoardListing>
{
    public void Configure(EntityTypeBuilder<LoadBoardListing> builder)
    {
        builder.ToTable("load_board_listings");

        builder.HasIndex(i => new { i.ExternalListingId, i.ProviderType })
            .IsUnique();

        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.ExpiresAt);

        builder.Property(i => i.ExternalListingId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.RatePerMile)
            .HasPrecision(18, 2);

        builder.ComplexProperty(i => i.TotalRate, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.Property(i => i.EquipmentType)
            .HasMaxLength(50);

        builder.Property(i => i.Commodity)
            .HasMaxLength(200);

        builder.Property(i => i.BrokerName)
            .HasMaxLength(200);

        builder.Property(i => i.BrokerPhone)
            .HasMaxLength(30);

        builder.Property(i => i.BrokerEmail)
            .HasMaxLength(200);

        builder.Property(i => i.BrokerMcNumber)
            .HasMaxLength(20);

        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        builder.HasOne(i => i.Load)
            .WithMany()
            .HasForeignKey(i => i.LoadId)
            .OnDelete(DeleteBehavior.SetNull);

        // Prefix address columns to avoid collision between OriginAddress and DestinationAddress
        // (both are complex [ComplexType] Address properties with the same 8 unprefixed column names).
        builder.ComplexProperty(i => i.OriginAddress, addr =>
        {
            addr.Property(a => a.Barangay).HasColumnName("origin_address_barangay");
            addr.Property(a => a.CityOrMunicipality).HasColumnName("origin_address_city_or_municipality");
            addr.Property(a => a.Country).HasColumnName("origin_address_country");
            addr.Property(a => a.Province).HasColumnName("origin_address_province");
            addr.Property(a => a.Region).HasColumnName("origin_address_region");
            addr.Property(a => a.StreetAddress).HasColumnName("origin_address_street_address");
            addr.Property(a => a.Subdivision).HasColumnName("origin_address_subdivision");
            addr.Property(a => a.Zipcode).HasColumnName("origin_address_zipcode");
        });

        builder.ComplexProperty(i => i.DestinationAddress, addr =>
        {
            addr.Property(a => a.Barangay).HasColumnName("destination_address_barangay");
            addr.Property(a => a.CityOrMunicipality).HasColumnName("destination_address_city_or_municipality");
            addr.Property(a => a.Country).HasColumnName("destination_address_country");
            addr.Property(a => a.Province).HasColumnName("destination_address_province");
            addr.Property(a => a.Region).HasColumnName("destination_address_region");
            addr.Property(a => a.StreetAddress).HasColumnName("destination_address_street_address");
            addr.Property(a => a.Subdivision).HasColumnName("destination_address_subdivision");
            addr.Property(a => a.Zipcode).HasColumnName("destination_address_zipcode");
        });
    }
}
