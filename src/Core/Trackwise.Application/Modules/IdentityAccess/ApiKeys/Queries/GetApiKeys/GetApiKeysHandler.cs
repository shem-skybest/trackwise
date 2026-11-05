using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.ApiKeys.Queries;

internal sealed class GetApiKeysHandler(
    ITenantUnitOfWork tenantUow) : IAppRequestHandler<GetApiKeysQuery, Result<List<ApiKeyDto>>>
{
    public async Task<Result<List<ApiKeyDto>>> Handle(GetApiKeysQuery req, CancellationToken ct)
    {
        var apiKeys = await tenantUow.Repository<ApiKey>()
            .GetListAsync(ct: ct);

        var dtos = apiKeys
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new ApiKeyDto(
                k.Id,
                k.Name,
                k.KeyPrefix,
                k.CreatedAt,
                k.LastUsedAt))
            .ToList();

        return Result<List<ApiKeyDto>>.Ok(dtos);
    }
}
