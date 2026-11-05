using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Queries;

internal sealed class GetAdminInvitationsHandler(IMasterUnitOfWork masterUow)
    : IAppRequestHandler<GetAdminInvitationsQuery, PagedResult<InvitationDto>>
{
    public async Task<PagedResult<InvitationDto>> Handle(GetAdminInvitationsQuery req, CancellationToken ct)
    {
        var query = masterUow.Repository<Invitation>()
            .Query()
            .Where(i => i.Type == InvitationType.AppUser && i.Status == InvitationStatus.Pending);

        if (!string.IsNullOrEmpty(req.Search))
        {
            var search = req.Search.ToLower();
            query = query.Where(i => i.Email.ToLower().Contains(search));
        }

        var totalItems = await query.CountAsync(ct);

        var invitations = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        // For app-user invitations Tenant is null, so the default mapper resolves TenantName to
        // empty and InvitedByName from InvitedByUser - no explicit overrides needed.
        var dtos = invitations.Select(i => i.ToDto()).ToList();

        return PagedResult<InvitationDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
