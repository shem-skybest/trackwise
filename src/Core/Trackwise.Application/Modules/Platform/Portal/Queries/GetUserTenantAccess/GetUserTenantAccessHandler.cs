using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Platform.Portal.Queries;

internal sealed class GetUserTenantAccessHandler(IMasterUnitOfWork masterUow)
    : IAppRequestHandler<GetUserTenantAccessQuery, Result<List<UserTenantAccessDto>>>
{
    public async Task<Result<List<UserTenantAccessDto>>> Handle(
        GetUserTenantAccessQuery req,
        CancellationToken ct)
    {
        var tenantAccessList = await masterUow.Repository<UserTenantAccess>().Query()
            .Where(uta => uta.UserId == req.UserId && uta.IsActive)
            .OrderByDescending(uta => uta.LastAccessedAt)
            .ToListAsync(ct);

        var dtos = tenantAccessList.Select(uta => uta.ToDto()).ToList();
        return Result<List<UserTenantAccessDto>>.Ok(dtos);
    }
}
