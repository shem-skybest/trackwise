using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class TenantRoleChangeLogEntityConfiguration : IEntityTypeConfiguration<TenantRoleChangeLog>
{
    public void Configure(EntityTypeBuilder<TenantRoleChangeLog> builder)
    {
        builder.ToTable("tenant_role_change_logs");
        builder.Property(i => i.RoleName).HasMaxLength(100);
        builder.Property(i => i.RoleDisplayName).HasMaxLength(100);
        builder.Property(i => i.Permission).HasMaxLength(150);
        builder.Property(i => i.PreviousRoleName).HasMaxLength(100);
        builder.Property(i => i.EmployeeName).HasMaxLength(200);
        builder.Property(i => i.ChangedByName).HasMaxLength(200);
        builder.HasIndex(i => i.ChangedAt);
        builder.HasIndex(i => i.EmployeeId);
    }
}
