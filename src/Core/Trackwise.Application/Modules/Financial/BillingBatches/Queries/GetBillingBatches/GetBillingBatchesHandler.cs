using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

internal sealed class GetBillingBatchesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBillingBatchesQuery, PagedResult<BillingBatchDto>>
{
    public async Task<PagedResult<BillingBatchDto>> Handle(GetBillingBatchesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<BillingBatch>().Query();

        if (req.Status is { } status)
        {
            query = query.Where(b => b.Status == status);
        }

        if (req.CustomerId is { } customerId)
        {
            query = query.Where(b => b.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            query = query.Where(b => b.ReferenceNumber.ToLower().Contains(search)
                || b.Customer.Name.ToLower().Contains(search));
        }

        var totalItems = await query.CountAsync(ct);

        var batches = await (string.IsNullOrWhiteSpace(req.OrderBy)
                ? query.OrderByDescending(b => b.PeriodStart).ThenByDescending(b => b.CreatedAt)
                : query.OrderBy(req.OrderBy))
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var customerIds = batches.Select(b => b.CustomerId).Distinct().ToList();
        var names = await tenantUow.Repository<Customer>().Query()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var dtos = batches.Select(b => b.ToDto(names.GetValueOrDefault(b.CustomerId))).ToArray();
        return PagedResult<BillingBatchDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
