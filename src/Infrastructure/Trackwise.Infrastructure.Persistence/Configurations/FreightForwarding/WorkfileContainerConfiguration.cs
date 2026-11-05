using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class WorkfileContainerConfiguration : IEntityTypeConfiguration<WorkfileContainer>
{
    public void Configure(EntityTypeBuilder<WorkfileContainer> builder)
    {
        builder.ToTable("workfile_containers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SealNumber).HasMaxLength(50);
        builder.Property(x => x.PackagingType).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.GrossWeightKg).HasPrecision(18, 2);
        builder.Property(x => x.Cbm).HasPrecision(18, 3);

        builder.HasOne<Workfile>()
            .WithMany(w => w.Containers)
            .HasForeignKey(x => x.WorkfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ContainerType>()
            .WithMany()
            .HasForeignKey(x => x.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict: a box that shipped on a file is history, so the register can't drop it.
        builder.HasOne<Container>()
            .WithMany()
            .HasForeignKey(x => x.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WorkfileId, x.SortOrder });
        builder.HasIndex(x => x.ContainerId);
    }
}
