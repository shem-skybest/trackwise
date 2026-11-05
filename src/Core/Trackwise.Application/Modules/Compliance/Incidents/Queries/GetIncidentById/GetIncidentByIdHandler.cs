using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Queries;

internal sealed class GetIncidentByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetIncidentByIdQuery, Result<IncidentDto>>
{
    public async Task<Result<IncidentDto>> Handle(GetIncidentByIdQuery req, CancellationToken ct)
    {
        var incident = await tenantUow.Repository<Incident>().GetByIdAsync(req.Id, ct);
        return incident is null
            ? Result<IncidentDto>.Fail($"Could not find the incident with ID '{req.Id}'.")
            : Result<IncidentDto>.Ok(await IncidentReader.ToDetailsDtoAsync(tenantUow, incident, ct));
    }
}
