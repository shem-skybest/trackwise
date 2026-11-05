using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class ResolveIncidentHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<ResolveIncidentCommand, Result<IncidentDto>>
{
    public Task<Result<IncidentDto>> Handle(ResolveIncidentCommand req, CancellationToken ct) =>
        IncidentEdit.ApplyAsync(tenantUow, req.Id,
            incident => incident.Resolve(req.CorrectiveAction, currentUser.GetUserName(), DateTime.UtcNow), ct);
}
