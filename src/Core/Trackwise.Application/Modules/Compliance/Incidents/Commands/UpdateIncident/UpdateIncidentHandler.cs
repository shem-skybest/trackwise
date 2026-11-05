using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class UpdateIncidentHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateIncidentCommand, Result<IncidentDto>>
{
    public Task<Result<IncidentDto>> Handle(UpdateIncidentCommand req, CancellationToken ct) =>
        IncidentEdit.ApplyAsync(tenantUow, req.Id, async incident =>
        {
            var parties = await IncidentPartiesResolver.ResolveAsync(tenantUow, req, ct);
            if (!parties.IsSuccess)
            {
                return Result.Fail(parties.Error!);
            }

            incident.Edit(req.ToDetails(), parties.Value!, DateTime.UtcNow);
            return null;
        }, ct);
}
