using Trackwise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Trackwise.Infrastructure.Persistence.Configurations;

internal sealed class AccountingProviderConfigurationEntityConfiguration
    : IEntityTypeConfiguration<AccountingProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<AccountingProviderConfiguration> builder)
    {
        builder.ToTable("accounting_provider_configurations");

        builder.HasIndex(i => i.ProviderType)
            .IsUnique();

        builder.Property(i => i.RealmId)
            .HasMaxLength(100);

        builder.Property(i => i.CompanyName)
            .HasMaxLength(256);

        builder.Property(i => i.DefaultPaymentAccountId)
            .HasMaxLength(100);

        builder.Property(i => i.DefaultExpenseAccountId)
            .HasMaxLength(100);

        // AccessToken / RefreshToken are encrypted at rest (EncryptedStringConverter) and stored
        // as unbounded text - ciphertext is larger than the plaintext token.
    }
}
