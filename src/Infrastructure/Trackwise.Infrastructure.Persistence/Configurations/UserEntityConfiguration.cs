using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(i => i.Tenant)
            .WithMany(i => i.Users)
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
