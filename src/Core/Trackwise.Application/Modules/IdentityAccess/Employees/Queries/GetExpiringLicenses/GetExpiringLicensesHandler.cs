using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

internal sealed class GetExpiringLicensesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetExpiringLicensesQuery, Result<IList<DriverLicenseDto>>>
{
    public async Task<Result<IList<DriverLicenseDto>>> Handle(
        GetExpiringLicensesQuery req, CancellationToken ct)
    {
        var threshold = DateTime.UtcNow.AddDays(req.WindowDays);

        var licenses = await tenantUow.Repository<DriverLicense>()
            .GetListAsync(
                i => i.Status == DriverLicenseStatus.Active
                     && i.ExpiresAt <= threshold,
                ct);

        var dtos = licenses
            .OrderBy(i => i.ExpiresAt)
            .Select(i => i.ToDto())
            .ToList();

        return Result<IList<DriverLicenseDto>>.Ok(dtos);
    }
}
