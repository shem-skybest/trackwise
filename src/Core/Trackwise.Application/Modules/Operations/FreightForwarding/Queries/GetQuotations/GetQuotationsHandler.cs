using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetQuotationsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetQuotationsQuery, PagedResult<QuotationSummaryDto>>
{
    public async Task<PagedResult<QuotationSummaryDto>> Handle(GetQuotationsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Quotation>().Query().AsNoTracking();

        if (req.CustomerId.HasValue)
            query = query.Where(q => q.CustomerId == req.CustomerId.Value);

        if (req.BranchId.HasValue)
            query = query.Where(q => q.BranchId == req.BranchId.Value);

        if (req.FreightMode.HasValue)
            query = query.Where(q => q.FreightMode == req.FreightMode.Value);

        if (req.Status.HasValue)
            query = query.Where(q => q.Status == req.Status.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var term = $"%{req.Search.Trim()}%";

            // Customer name isn't on Quotation, so match it through a subquery
            var matchingCustomerIds = tenantUow.Repository<Customer>().Query()
                .Where(c => EF.Functions.Like(c.Name, term))
                .Select(c => c.Id);

            query = query.Where(q =>
                EF.Functions.Like(q.ReferenceNumber, term) ||
                matchingCustomerIds.Contains(q.CustomerId));
        }

        var totalItems = await query.CountAsync(ct);

        // Materialized, not projected - TotalSellingAmount needs QuotationLine.Price() per line,
        // which runs in-memory against the eager-loaded TariffRate/ChargeCode.
        var quotations = await query
            .OrderByDescending(q => q.CreatedAt)
            .ThenBy(q => q.Id)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Include(q => q.Lines).ThenInclude(l => l.TariffRate!).ThenInclude(r => r!.ChargeCode)
            .Include(q => q.Lines).ThenInclude(l => l.TariffRate!).ThenInclude(r => r!.Tariff)
            .ToListAsync(ct);

        var customerIds = quotations.Select(q => q.CustomerId).Distinct().ToList();
        var customerNames = await tenantUow.Repository<Customer>().Query()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var branchIds = quotations.Select(q => q.BranchId).Distinct().ToList();
        var branchNames = await tenantUow.Repository<Branch>().Query()
            .Where(b => branchIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Name })
            .ToDictionaryAsync(b => b.Id, b => b.Name, ct);

        var dtos = quotations
            .Select(q =>
            {
                var totalSelling = q.Lines.Sum(l => l.Price().Selling.Amount);
                var customerName = customerNames.GetValueOrDefault(q.CustomerId, "Unknown");
                var branchName = branchNames.GetValueOrDefault(q.BranchId, "Unknown Branch");
                return q.ToSummaryDto(customerName, branchName, q.Lines.Count, totalSelling);
            })
            .ToList();

        return PagedResult<QuotationSummaryDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
