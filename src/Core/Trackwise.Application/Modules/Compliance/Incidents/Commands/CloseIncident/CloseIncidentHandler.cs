using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class CloseIncidentHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<CloseIncidentCommand, Result<IncidentDto>>
{
    public Task<Result<IncidentDto>> Handle(CloseIncidentCommand req, CancellationToken ct) =>
        IncidentEdit.ApplyAsync(tenantUow, req.Id,
            incident => incident.Close(currentUser.GetUserName(), DateTime.UtcNow), ct);
}
