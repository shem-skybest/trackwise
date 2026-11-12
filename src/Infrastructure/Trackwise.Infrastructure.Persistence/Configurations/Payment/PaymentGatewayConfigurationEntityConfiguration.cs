using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class PaymentGatewayConfigurationEntityConfiguration
    : IEntityTypeConfiguration<PaymentGatewayConfiguration>
{
    public void Configure(EntityTypeBuilder<PaymentGatewayConfiguration> builder)
    {
        builder.ToTable("payment_gateway_configurations");

        builder.HasIndex(i => i.Gateway).IsUnique();

        // Secret columns are encrypted at rest (EncryptedStringConverter); ciphertext is larger
        // than the plaintext, so they stay unbounded text.
        builder.Property(i => i.SecretKey).IsRequired();
        builder.Property(i => i.CallbackToken).IsRequired();

        builder.Ignore(i => i.IsTestMode);
    }
}
