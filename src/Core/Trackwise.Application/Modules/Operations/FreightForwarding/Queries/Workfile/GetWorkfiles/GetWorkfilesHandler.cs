using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetWorkfilesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetWorkfilesQuery, PagedResult<WorkfileSummaryDto>>
{
    public async Task<PagedResult<WorkfileSummaryDto>> Handle(GetWorkfilesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Workfile>().Query().AsNoTracking();

        if (!string.IsNullOrEmpty(req.Search))
        {
            query = query.Where(w =>
                w.ReferenceNumber.Contains(req.Search) ||
                (w.BillOfLadingNumber != null && w.BillOfLadingNumber.Contains(req.Search)));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(w => w.Type == req.Type.Value);
        }

        if (req.Status.HasValue)
        {
            query = query.Where(w => w.Status == req.Status.Value);
        }

        if (req.CustomerId.HasValue)
        {
            query = query.Where(w => w.CustomerId == req.CustomerId.Value);
        }

        if (req.IsForwardedToAccounting.HasValue)
        {
            query = query.Where(w => w.IsForwardedToAccounting == req.IsForwardedToAccounting.Value);
        }

        var totalItems = await query.CountAsync(ct);

        var workfiles = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        // One lookup for the page rather than a lazy load per row.
        var customerIds = workfiles.Select(w => w.CustomerId).Distinct().ToArray();
        var customerNames = customerIds.Length == 0
            ? []
            : await tenantUow.Repository<Customer>().Query()
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var items = workfiles
            .Select(w => w.ToSummaryDto(customerNames.GetValueOrDefault(w.CustomerId, string.Empty)))
            .ToArray();

        return PagedResult<WorkfileSummaryDto>.Ok(items, totalItems, req.PageSize);
    }
}
