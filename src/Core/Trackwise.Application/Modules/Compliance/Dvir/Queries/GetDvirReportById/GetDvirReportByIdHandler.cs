using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Queries;

internal sealed class GetDvirReportByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetDvirReportByIdQuery, Result<DvirReportDto>>
{
    public async Task<Result<DvirReportDto>> Handle(GetDvirReportByIdQuery req, CancellationToken ct)
    {
        var entity = await tenantUow.Repository<DvirReport>().GetByIdAsync(req.Id, ct);

        if (entity is null)
        {
            return Result<DvirReportDto>.Fail($"Could not find a DVIR report with ID '{req.Id}'");
        }

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, entity.TruckId, ct);
        return Result<DvirReportDto>.Ok(entity.ToDto(truckNumber));
    }
}
