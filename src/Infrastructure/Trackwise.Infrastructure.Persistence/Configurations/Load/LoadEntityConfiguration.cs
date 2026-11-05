using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class LoadEntityConfiguration : IEntityTypeConfiguration<Load>
{
    public void Configure(EntityTypeBuilder<Load> builder)
    {
        builder.ToTable("loads");

        builder.ComplexProperty(i => i.DeliveryCost, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.Property(i => i.Number)
            .UseIdentityAlwaysColumn()
            .IsRequired()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(i => i.Number)
            .IsUnique();

        builder.HasOne(i => i.AssignedDispatcher)
            .WithMany(i => i.DispatchedLoads)
            .HasForeignKey(i => i.AssignedDispatcherId);
        //.OnDelete(DeleteBehavior.SetNull);

        // External load board source tracking
        builder.Property(i => i.ExternalSourceId)
            .HasMaxLength(100);

        builder.Property(i => i.ExternalBrokerReference)
            .HasMaxLength(100);

        // Order-intake & intermodal fields
        builder.Property(i => i.Notes)
            .HasMaxLength(2000);

        // Cargo and drop outcome
        builder.Property(i => i.Commodity)
            .HasMaxLength(100);
        builder.Property(i => i.OutcomeNote)
            .HasMaxLength(500);

        // Hazmat / ADR
        builder.Property(i => i.UnNumber)
            .HasMaxLength(16);

        builder.HasOne(i => i.Container)
            .WithMany()
            .HasForeignKey(i => i.ContainerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.OriginTerminal)
            .WithMany()
            .HasForeignKey(i => i.OriginTerminalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.DestinationTerminal)
            .WithMany()
            .HasForeignKey(i => i.DestinationTerminalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.OriginSite)
            .WithMany()
            .HasForeignKey(i => i.OriginSiteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.DestinationSite)
            .WithMany()
            .HasForeignKey(i => i.DestinationSiteId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Booking)
            .WithMany()
            .HasForeignKey(i => i.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Workfile>()
            .WithMany()
            .HasForeignKey(i => i.WorkfileId)
            .OnDelete(DeleteBehavior.Restrict);

        // SetNull: only a cancelled leg's loads can still point at a slot being removed
        // (RemoveWorkfileContainerHandler refuses while a live leg moves it).
        builder.HasOne<WorkfileContainer>()
            .WithMany()
            .HasForeignKey(i => i.WorkfileContainerId)
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

        // Computed properties - keep out of EF mapping
        builder.Ignore(i => i.CanConfirmPickUp);
        builder.Ignore(i => i.CanConfirmDelivery);
    }
}
