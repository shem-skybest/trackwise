using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetBusinessPartnersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBusinessPartnersQuery, PagedResult<BusinessPartnerDto>>
{
    public async Task<PagedResult<BusinessPartnerDto>> Handle(
        GetBusinessPartnersQuery req, CancellationToken ct)
    {
        // Approved records only, and not as a default a caller can turn off. This is the feed the
        // workfile's counterparty pickers read, and a draft partner exists precisely because nobody
        // has agreed it is a company yet - offering it for selection would put it on a live file
        // and make the approval step something a clerk can walk around. The steward's own list
        // (GetPartnersQuery) is where drafts belong.
        var query = tenantUow.Repository<BusinessPartner>().Query()
            .Where(i => i.ApprovalState == MasterDataApprovalState.Approved);

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Code is stored canonical; Name is free-form, matched lowercased because Postgres LIKE is case-sensitive.
            var code = BusinessPartner.NormalizeCode(req.Search);
            var search = req.Search.Trim().ToLower();

            query = query.Where(i => i.Name.ToLower().Contains(search) || i.Code.Contains(code));
        }

        if (req.Role.HasValue)
        {
            query = query.Where(i => i.Roles.Contains(req.Role.Value));
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(i => i.IsActive == req.IsActive.Value);
        }

        var totalItems = await query.CountAsync(ct);

        var partners = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        return PagedResult<BusinessPartnerDto>.Ok(
            partners.Select(i => i.ToDto()).ToArray(), totalItems, req.PageSize);
    }
}
