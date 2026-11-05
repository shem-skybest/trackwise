using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Queries;

internal sealed class GetPaymentsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetPaymentsQuery, PagedResult<PaymentDto>>
{
    public Task<PagedResult<PaymentDto>> Handle(
        GetPaymentsQuery req,
        CancellationToken ct)
    {
        var query = tenantUow.Repository<Payment>().Query()
            .Where(i => i.CreatedAt >= req.StartDate && i.CreatedAt <= req.EndDate);

        var totalItems = query.Count();

        var payments = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(i => i.ToDto())
            .ToArray();

        return Task.FromResult(PagedResult<PaymentDto>.Ok(payments, totalItems, req.PageSize));
    }
}
