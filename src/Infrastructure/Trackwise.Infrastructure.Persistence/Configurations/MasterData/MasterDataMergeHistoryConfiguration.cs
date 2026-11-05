using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class MasterDataMergeHistoryConfiguration : IEntityTypeConfiguration<MasterDataMergeHistory>
{
    public void Configure(EntityTypeBuilder<MasterDataMergeHistory> builder)
    {
        builder.ToTable("master_data_merge_histories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LoserCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.LoserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.MergedBy).HasMaxLength(50);

        builder.Property(x => x.FieldChoices).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.EntityType, x.WinnerId });
        builder.HasIndex(x => x.LoserId);
    }
}
