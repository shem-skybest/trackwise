using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class ReopenIncidentHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ReopenIncidentCommand, Result<IncidentDto>>
{
    public Task<Result<IncidentDto>> Handle(ReopenIncidentCommand req, CancellationToken ct) =>
        IncidentEdit.ApplyAsync(tenantUow, req.Id, incident => incident.Reopen(req.Reason), ct);
}
