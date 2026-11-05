using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

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

        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);

        // The zone a location sits in. Restrict for the same reason as the branch link: retiring a
        // zone goes through the dependency probe so the steward is told what is inside it, and a
        // cascade would take the delivery points with it.
        builder.HasOne(x => x.ParentLocation)
            .WithMany()
            .HasForeignKey(x => x.ParentLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentLocationId);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);

        // Cascade to match the customer's loads and invoices, which already go with it. Restrict
        // here would only turn a customer delete into an unhandled FK violation.
        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CustomerId);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UpdatedAt);
    }
}
