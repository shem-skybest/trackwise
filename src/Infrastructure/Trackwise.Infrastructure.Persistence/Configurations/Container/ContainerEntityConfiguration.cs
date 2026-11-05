using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class ContainerEntityConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("containers");

        builder.Property(c => c.Number)
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(c => c.Number).IsUnique();

        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasOne(c => c.ContainerType)
            .WithMany()
            .HasForeignKey(c => c.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.ContainerTypeId);

        builder.HasOne(c => c.CurrentTerminal)
            .WithMany()
            .HasForeignKey(c => c.CurrentTerminalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
