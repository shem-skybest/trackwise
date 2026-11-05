using Trackwise.Domain.Core;
using Trackwise.Infrastructure.Persistence.Converters;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Extensions;

public static class EncryptedColumnsExtensions
{
    /// <summary>
    ///     Data Protection purpose for provider secret columns. Stable string - do not change,
    ///     or previously encrypted values become undecryptable.
    /// </summary>
    private const string ProtectorPurpose = "Trackwise.ProviderSecrets.v1";

    /// <summary>
    ///     Encrypt at rest every string property flagged with <see cref="EncryptedSecretAttribute"/>
    ///     across the model. Applied after entity configurations so it overrides their string
    ///     mappings. Driven off the attribute (like the auditable/tenant marker conventions in this
    ///     folder) so a new provider-config secret column is encrypted by annotation alone.
    ///     If <paramref name="dataProtectionProvider"/> is null (e.g. design-time / tests),
    ///     columns stay plaintext - the converter never changes the store type, so the schema
    ///     is identical either way.
    /// </summary>
    public static void ApplyEncryptedSecretColumns(
        this ModelBuilder builder,
        IDataProtectionProvider? dataProtectionProvider)
    {
        if (dataProtectionProvider is null)
        {
            return;
        }

        var converter = new EncryptedStringConverter(
            dataProtectionProvider.CreateProtector(ProtectorPurpose));

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) &&
                    property.PropertyInfo?.IsDefined(typeof(EncryptedSecretAttribute), inherit: false) == true)
                {
                    property.SetValueConverter(converter);
                }
            }
        }
    }
}
