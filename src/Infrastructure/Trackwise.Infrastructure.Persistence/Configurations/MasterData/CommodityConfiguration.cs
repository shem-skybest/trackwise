using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class CommodityConfiguration : IEntityTypeConfiguration<Commodity>
{
    public void Configure(EntityTypeBuilder<Commodity> builder)
    {
        builder.ToTable("commodities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Deliberately NOT unique. An HS code classifies a family of goods for customs, so two
        // genuinely different commodity lines can share one - making it unique would force a
        // steward to invent a distinction the tariff schedule does not have.
        builder.Property(x => x.HsCode).HasMaxLength(20);
        builder.HasIndex(x => x.HsCode);

        builder.Property(x => x.UnNumber).HasMaxLength(10);

        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.IsHazardous).IsRequired();
        builder.Property(x => x.RequiresTemperatureControl).IsRequired();

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);

        builder.HasOne<Commodity>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UpdatedAt);
    }
}
