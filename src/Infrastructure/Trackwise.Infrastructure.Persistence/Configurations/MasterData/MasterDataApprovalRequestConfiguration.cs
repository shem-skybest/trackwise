using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.MasterData;

internal sealed class MasterDataApprovalRequestConfiguration
    : IEntityTypeConfiguration<MasterDataApprovalRequest>
{
    public void Configure(EntityTypeBuilder<MasterDataApprovalRequest> builder)
    {
        builder.ToTable("master_data_approval_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(200).IsRequired();

        // 50 to match every other actor column in the schema (created_by, deleted_by, changed_by):
        // these hold a user id, and the display names beside them are snapshots for reading.
        builder.Property(x => x.SubmittedBy).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReviewedBy).HasMaxLength(50);
        builder.Property(x => x.SubmittedByName).HasMaxLength(200);
        builder.Property(x => x.ReviewedByName).HasMaxLength(200);

        builder.Property(x => x.SubmissionNote).HasMaxLength(1000);
        builder.Property(x => x.ReviewNote).HasMaxLength(1000);

        builder.Ignore(x => x.IsOpen);

        // The queue's index, and the reason Decision is nullable rather than a three-valued enum:
        // "waiting on somebody" is a partial index over a handful of rows in a table that only ever
        // grows, not a scan for a status member.
        builder.HasIndex(x => x.SubmittedAt)
            .HasFilter("decision IS NULL")
            .HasDatabaseName("ix_master_data_approval_requests_open");

        // One record's whole approval history, newest first - submitted, sent back, submitted again.
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}
