using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class NumberSeriesConfiguration : IEntityTypeConfiguration<NumberSeries>
{
    public void Configure(EntityTypeBuilder<NumberSeries> builder)
    {
        builder.ToTable("number_series");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Series).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Prefix).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Separator).HasMaxLength(4).IsRequired();

        // The allocating statement is an upsert that conflicts on this index - without it being
        // unique, ON CONFLICT has nothing to target and two stewards can take the same number.
        builder.HasIndex(x => x.Series).IsUnique();
    }
}
