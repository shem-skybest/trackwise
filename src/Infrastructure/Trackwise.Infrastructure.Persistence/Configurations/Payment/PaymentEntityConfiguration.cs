using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class PaymentEntityConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.ComplexProperty(i => i.Amount, money =>
        {
            money.Property(m => m.Amount).HasPrecision(18, 2);
            money.Property(m => m.Currency).HasMaxLength(3);
        });

        builder.Property(i => i.GatewayReference).HasMaxLength(100);

        // A gateway callback is retried until we answer 200; this is what stops a retry from
        // recording the same payment twice.
        builder.HasIndex(i => new { i.Gateway, i.GatewayReference })
            .IsUnique()
            .HasFilter("gateway_reference IS NOT NULL");
    }
}
