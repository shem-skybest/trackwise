using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class ReviewIncidentHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<ReviewIncidentCommand, Result<IncidentDto>>
{
    public Task<Result<IncidentDto>> Handle(ReviewIncidentCommand req, CancellationToken ct) =>
        IncidentEdit.ApplyAsync(tenantUow, req.Id,
            incident => incident.StartReview(req.Notes, currentUser.GetUserName(), DateTime.UtcNow), ct);
}
