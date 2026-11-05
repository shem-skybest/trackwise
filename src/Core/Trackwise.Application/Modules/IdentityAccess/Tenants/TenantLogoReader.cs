using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants;

/// <summary>
/// Reads the logo uploaded in Settings > Company so a printed PDF can carry it on its letterhead.
/// A logo that is missing or unreadable returns null: the document prints without it rather than
/// failing the whole download.
/// </summary>
internal static class TenantLogoReader
{
    public static async Task<byte[]?> ReadAsync(
        IBlobStorageService blobStorage, Tenant tenant, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(tenant.LogoPath))
        {
            return null;
        }

        try
        {
            await using var stream = await blobStorage.DownloadAsync(BlobConstants.LogosContainerName, tenant.LogoPath, ct);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            return buffer.ToArray();
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }
}
