using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class MasterDataChangeLogConfiguration : IEntityTypeConfiguration<MasterDataChangeLog>
{
    public void Configure(EntityTypeBuilder<MasterDataChangeLog> builder)
    {
        builder.ToTable("master_data_change_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityCode).HasMaxLength(64);
        builder.Property(x => x.Field).HasMaxLength(128);
        builder.Property(x => x.ChangedBy).HasMaxLength(50);

        // Values are stored as rendered text, not typed columns: the log spans every master entity
        // and has to hold whatever a field contained. Left unbounded because a notes or address
        // field can be long, and a truncated "before" value defeats the point of keeping it.
        builder.Property(x => x.OldValue);
        builder.Property(x => x.NewValue);

        // The one query the UI makes: this record's history, newest first.
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.ChangedAt })
            .IsDescending(false, false, true);
    }
}
