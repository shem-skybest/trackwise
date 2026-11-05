namespace Trackwise.Domain.Core;

/// <summary>
///     Marks a string entity property as a reversible secret that must be encrypted at rest.
///     The persistence layer applies an encrypting value converter to every property carrying
///     this attribute (see <c>EncryptedColumnsExtensions.ApplyEncryptedSecretColumns</c>), so a
///     new provider-config column is encrypted by annotation alone - no central list to update.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class EncryptedSecretAttribute : Attribute;
