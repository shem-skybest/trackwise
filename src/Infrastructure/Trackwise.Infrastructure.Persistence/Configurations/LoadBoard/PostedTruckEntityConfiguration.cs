using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class PostedTruckEntityConfiguration : IEntityTypeConfiguration<PostedTruck>
{
    public void Configure(EntityTypeBuilder<PostedTruck> builder)
    {
        builder.ToTable("posted_trucks");

        builder.HasIndex(i => new { i.TruckId, i.ProviderType })
            .IsUnique();

        builder.HasIndex(i => i.ExternalPostId);
        builder.HasIndex(i => i.Status);

        builder.Property(i => i.ExternalPostId)
            .HasMaxLength(100);

        builder.Property(i => i.EquipmentType)
            .HasMaxLength(50);

        builder.HasOne(i => i.Truck)
            .WithMany()
            .HasForeignKey(i => i.TruckId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prefix address columns to avoid collision between AvailableAtAddress and DestinationPreference
        // (both are complex [ComplexType] Address properties with the same 8 unprefixed column names).
        builder.ComplexProperty(i => i.AvailableAtAddress, addr =>
        {
            addr.Property(a => a.Barangay).HasColumnName("available_at_address_barangay");
            addr.Property(a => a.CityOrMunicipality).HasColumnName("available_at_address_city_or_municipality");
            addr.Property(a => a.Country).HasColumnName("available_at_address_country");
            addr.Property(a => a.Province).HasColumnName("available_at_address_province");
            addr.Property(a => a.Region).HasColumnName("available_at_address_region");
            addr.Property(a => a.StreetAddress).HasColumnName("available_at_address_street_address");
            addr.Property(a => a.Subdivision).HasColumnName("available_at_address_subdivision");
            addr.Property(a => a.Zipcode).HasColumnName("available_at_address_zipcode");
        });

        builder.ComplexProperty(i => i.DestinationPreference, addr =>
        {
            addr.Property(a => a.Barangay).HasColumnName("destination_preference_barangay");
            addr.Property(a => a.CityOrMunicipality).HasColumnName("destination_preference_city_or_municipality");
            addr.Property(a => a.Country).HasColumnName("destination_preference_country");
            addr.Property(a => a.Province).HasColumnName("destination_preference_province");
            addr.Property(a => a.Region).HasColumnName("destination_preference_region");
            addr.Property(a => a.StreetAddress).HasColumnName("destination_preference_street_address");
            addr.Property(a => a.Subdivision).HasColumnName("destination_preference_subdivision");
            addr.Property(a => a.Zipcode).HasColumnName("destination_preference_zipcode");
        });
    }
}
