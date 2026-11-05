using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class MasterDataXrefConfiguration : IEntityTypeConfiguration<MasterDataXref>
{
    public void Configure(EntityTypeBuilder<MasterDataXref> builder)
    {
        builder.ToTable("master_data_xrefs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceTable).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SourceKey).HasMaxLength(512).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MatchNote).HasMaxLength(1000);

        // One legacy row resolves to exactly one canonical record. Many rows may resolve to the
        // same record - that is what a merge produces - but never the reverse.
        builder.HasIndex(x => new { x.SourceSystem, x.SourceTable, x.SourceKey })
            .IsUnique();

        // The reverse lookup: every legacy key currently pointing at a record. Read on every merge
        // to repoint them, and by the reconciliation report.
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
