using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.ToTable("tariffs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired(); // ISO 4217

        builder.Property(x => x.Mode)
            .IsRequired();

        builder.Property(x => x.ServiceLevel)
            .IsRequired();

        builder.Property(x => x.OriginLane)
            .HasMaxLength(50);

        builder.Property(x => x.DestinationLane)
            .HasMaxLength(50);

        builder.HasOne(x => x.ContainerType)
            .WithMany()
            .HasForeignKey(x => x.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ContainerTypeId);
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .IsRequired();

        builder.Property(x => x.EffectiveTo)
            .IsRequired(false);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // SupersedesTariffId
        builder.Property(x => x.SupersedesTariffId)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(x => new { x.Name, x.Mode, x.ServiceLevel, x.ContractId, x.CustomerId, x.IsActive })
            .IsUnique();

        // Relationships (we'll configure FKs in the infrastructure layer if needed)
    }
}
