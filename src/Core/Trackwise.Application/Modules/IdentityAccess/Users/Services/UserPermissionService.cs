using Trackwise.Application.Modules.IdentityAccess.Users.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Services;

internal sealed class UserPermissionService(IMediator mediator, IMemoryCache cache)
    : IUserPermissionService
{
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(
        Guid userId, Guid? tenantId, CancellationToken ct = default)
    {
        var tenantKey = tenantId?.ToString() ?? "no-tenant";
        var cacheKey = $"permissions:{tenantKey}:{GetGeneration(tenantKey)}:{userId}";
        if (cache.TryGetValue(cacheKey, out HashSet<string>? cached) && cached is not null)
            return cached;

        var result = await mediator.Send(
            new GetCurrentUserPermissionsQuery { UserId = userId, TenantId = tenantId }, ct);

        // A failed lookup is not cached: a transient database error would otherwise lock the user
        // out of every permission-gated action for the whole expiry window.
        if (!result.IsSuccess)
            return new HashSet<string>();

        var permissions = result.Value!.ToHashSet();
        cache.Set(cacheKey, permissions, CacheExpiry);
        return permissions;
    }

    public void InvalidateTenant(Guid tenantId)
    {
        var tenantKey = tenantId.ToString();
        cache.Set(GenerationKey(tenantKey), GetGeneration(tenantKey) + 1);
    }

    // Bumping the tenant's generation orphans every cached entry for it at once - IMemoryCache has
    // no prefix eviction - and the orphans age out on their own expiry.
    private long GetGeneration(string tenantKey) =>
        cache.TryGetValue(GenerationKey(tenantKey), out long generation) ? generation : 0;

    private static string GenerationKey(string tenantKey) => $"permissions-generation:{tenantKey}";

    public async Task<bool> HasPermissionAsync(
        Guid userId, Guid? tenantId, string permission, CancellationToken ct = default) =>
        (await GetPermissionsAsync(userId, tenantId, ct)).Contains(permission);
}
