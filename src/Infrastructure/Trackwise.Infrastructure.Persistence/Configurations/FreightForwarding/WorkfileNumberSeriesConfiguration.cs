using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class WorkfileNumberSeriesConfiguration : IEntityTypeConfiguration<WorkfileNumberSeries>
{
    public void Configure(EntityTypeBuilder<WorkfileNumberSeries> builder)
    {
        builder.ToTable("workfile_number_series");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Series)
            .HasMaxLength(20)
            .IsRequired();

        // The allocating UPSERT targets this constraint by name via ON CONFLICT (series).
        builder.HasIndex(x => x.Series).IsUnique();

        builder.Property(x => x.LastAllocated).IsRequired();
    }
}
