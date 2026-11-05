using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Queries;

internal sealed class GetCustomersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetCustomersQuery, PagedResult<CustomerDto>>
{
    public Task<PagedResult<CustomerDto>> Handle(GetCustomersQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Customer>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Plain `.Contains(search)` is case-sensitive (Postgres LIKE), and the
            // `StringComparison` overload doesn't translate at all - Npgsql's EF provider throws
            // at query time. Application can't reference the Npgsql package for `EF.Functions.ILike`
            // either (see csharp-conventions.md layering), so lowercase both sides instead - this is
            // the pattern `GetInvitationsHandler`/`GetAdminInvitationsHandler` already use.
            var search = req.Search.ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(search));
        }

        var totalItems = query.Count();

        var customers = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(i => i.ToDto())
            .ToArray();

        return Task.FromResult(PagedResult<CustomerDto>.Ok(customers, totalItems, req.PageSize));
    }
}
