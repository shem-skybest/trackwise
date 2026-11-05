using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetPartnersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetPartnersQuery, PagedResult<BusinessPartnerDto>>
{
    public async Task<PagedResult<BusinessPartnerDto>> Handle(
        GetPartnersQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<BusinessPartner>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Three stored forms, three shapes of search term. Code is canonical upper-case, the
            // TIN column holds digits only, and the name keeps whatever the steward typed - so
            // pasting "123-456-789-000" finds the company and typing "maersk" finds it too.
            var code = BusinessPartner.NormalizeCode(req.Search);
            var tin = BusinessPartner.NormalizeTaxId(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(p =>
                p.Code.Contains(code)
                || p.Name.ToLower().Contains(search)
                || (tin != null && p.TaxId != null && p.TaxId.Contains(tin)));
        }

        if (req.Role.HasValue)
        {
            query = query.Where(p => p.Roles.Contains(req.Role.Value));
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == req.IsActive.Value);
        }

        if (req.ApprovalState is { } approvalState)
        {
            query = query.Where(p => p.ApprovalState == approvalState);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the partners the branch actually trades with.
            query = query.Where(p => p.BranchId == branchId || p.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(p => (p.UpdatedAt ?? p.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var partners = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, partners.Select(p => p.BranchId), ct);

        var approvals = await MasterDataApprovalLookup.ResolveAsync(
            tenantUow, nameof(BusinessPartner), partners.Select(p => p.Id), ct);

        var dtos = partners
            .Select(p =>
            {
                var dto = p.ToDto(branchNames);
                MasterDataApprovalLookup.Apply(dto, approvals);
                return dto;
            })
            .ToArray();

        return PagedResult<BusinessPartnerDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
