using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Storage;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Queries;

internal sealed class GetTenantHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IBlobStorageService blobStorageService)
    : IAppRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(
        GetTenantQuery req, CancellationToken ct)
    {
        var tenantEntity = await masterUow.Repository<Tenant>().GetAsync(i => i.Id == req.Id || i.Name == req.Name, ct);

        if (tenantEntity is null)
        {
            return Result<TenantDto>.Fail($"Could not find a tenant with ID or Name: {req.Id} or {req.Name}");
        }

        // Count the number of trucks belonging to the tenant
        await tenantUow.SetCurrentTenantByIdAsync(tenantEntity.Id);
        var truckCount = await tenantUow.Repository<Truck>().CountAsync(ct: ct);

        var tenantDto = tenantEntity.ToDto(req.IncludeConnectionString, truckCount);

        // The mapper copies the raw storage blob path - resolve it to a servable URL here,
        // same as GetPublicTrackingHandler does for the public tracking page's logo.
        if (!string.IsNullOrEmpty(tenantDto.LogoUrl) &&
            !tenantDto.LogoUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            tenantDto = tenantDto with
            {
                LogoUrl = blobStorageService.GetPublicUrl(
                    BlobConstants.LogosContainerName, tenantDto.LogoUrl, tenantEntity.Id)
            };
        }

        return Result<TenantDto>.Ok(tenantDto);
    }
}
