using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations.FreightForwarding;

internal sealed class ChargeCodeConfiguration : IEntityTypeConfiguration<ChargeCode>
{
    public void Configure(EntityTypeBuilder<ChargeCode> builder)
    {
        builder.ToTable("charge_codes");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.Category)
            .IsRequired();

        builder.Property(x => x.DefaultUnit)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RequiresApproval)
            .IsRequired();

        builder.Property(x => x.TaxCode)
            .HasMaxLength(50);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.CreatedById)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3);

        // ApplicableModes as array - store as JSON (text for PostgreSQL)
        builder.Property(x => x.ApplicableModes)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<FreightMode[]>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? Array.Empty<FreightMode>())
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(x => new { x.TenantId, x.Code, x.IsActive });
        builder.HasIndex(x => new { x.TenantId, x.Category });
    }
}
