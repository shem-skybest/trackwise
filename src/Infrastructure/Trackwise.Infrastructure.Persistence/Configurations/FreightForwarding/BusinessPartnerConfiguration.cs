using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class BusinessPartnerConfiguration : IEntityTypeConfiguration<BusinessPartner>
{
    public void Configure(EntityTypeBuilder<BusinessPartner> builder)
    {
        builder.ToTable("business_partners");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(30)
            .IsRequired();

        // Filtered rather than plain unique, as on every other master entity: a soft-deleted record
        // keeps its code, and an unfiltered index would let it hold that code hostage forever.
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Stored as text[] of snake_case values so roles read the same as every other enum column
        // and `Roles.Contains(role)` still translates to SQL. SnakeCaseEnumConvention only reaches
        // scalar enum properties, so the element converter has to be applied by hand here.
        builder.PrimitiveCollection(x => x.Roles)
            .ElementType(e => e.HasConversion<SnakeCaseEnumConverter<BusinessPartnerRole>>());

        builder.Property(x => x.ContactName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.TaxId).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        // ⚠️ Deliberately NOT unique, which is where a TIN parts company with a vessel's IMO number
        // even though both are exact duplicate rules. A unique index would make the probe's override
        // unreachable: a steward who has looked at the candidate and decided these really are two
        // records would hit a database error with no field attached to it, and inside a bulk import
        // that error cannot be attributed to a row at all. The probe refuses the write and asks for
        // a reason instead, which is the same protection with a door in it.
        builder.HasIndex(x => x.TaxId).HasFilter("tax_id IS NOT NULL");

        builder.Property(x => x.IsActive).IsRequired();

        // Stored, unlike every other part of MasterDataState. Nothing on a record distinguishes a
        // draft from a submission waiting on somebody, and both look exactly like an inactive
        // record - see MasterDataApprovalState.
        builder.Property(x => x.ApprovalState).IsRequired();

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Restrict, not cascade: retiring a branch must go through the dependency probe, which tells
        // the steward what is scoped to it. A cascade would silently take the partners with it.
        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BranchId);

        builder.HasOne<BusinessPartner>()
            .WithMany()
            .HasForeignKey(x => x.MergedIntoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.IsActive);

        // The partner list's own filter, and the approvers' entry point into it.
        builder.HasIndex(x => x.ApprovalState);

        // Serves the delta feed other systems pull with ?updated_since=.
        builder.HasIndex(x => x.UpdatedAt);
    }
}
