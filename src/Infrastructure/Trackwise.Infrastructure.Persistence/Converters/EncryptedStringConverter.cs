using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Trackwise.Infrastructure.Persistence.Converters;

/// <summary>
///     EF Core value converter that encrypts a string column at rest using ASP.NET Core
///     Data Protection. Ciphertext is stored with an "enc:v1:" prefix so that:
///     <list type="bullet">
///         <item>legacy plaintext values (written before encryption existed) are read back
///         unchanged, so nothing breaks during rollout;</item>
///         <item>re-saving an already-encrypted value does not double-encrypt it (idempotent
///         back-fill).</item>
///     </list>
///     EF applies converters only to non-null values, so null handling is not needed here.
/// </summary>
public sealed class EncryptedStringConverter : ValueConverter<string, string>
{
    public const string Prefix = "enc:v1:";

    public EncryptedStringConverter(IDataProtector protector)
        : base(
            plaintext => Encrypt(protector, plaintext),
            stored => Decrypt(protector, stored))
    {
    }

    private static string Encrypt(IDataProtector protector, string plaintext)
    {
        // Guard against double-encryption during idempotent back-fill re-saves.
        return plaintext.StartsWith(Prefix, StringComparison.Ordinal)
            ? plaintext
            : Prefix + protector.Protect(plaintext);
    }

    private static string Decrypt(IDataProtector protector, string stored)
    {
        // Values written before encryption was introduced have no prefix - pass through.
        return stored.StartsWith(Prefix, StringComparison.Ordinal)
            ? protector.Unprotect(stored[Prefix.Length..])
            : stored;
    }
}
