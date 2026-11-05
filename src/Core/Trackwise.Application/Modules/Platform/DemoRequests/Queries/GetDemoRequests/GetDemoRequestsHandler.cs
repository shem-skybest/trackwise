using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Queries;

internal sealed class GetDemoRequestsHandler(
    IMasterUnitOfWork masterUow) : IAppRequestHandler<GetDemoRequestsQuery, PagedResult<DemoRequestDto>>
{
    public Task<PagedResult<DemoRequestDto>> Handle(GetDemoRequestsQuery req, CancellationToken ct)
    {
        var query = masterUow.Repository<DemoRequest>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            query = query.Where(x =>
                x.Email.Contains(req.Search) ||
                x.FirstName.Contains(req.Search) ||
                x.LastName.Contains(req.Search) ||
                x.Company.Contains(req.Search));
        }

        var totalItems = query.Count();

        var items = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(i => i.ToDto())
            .ToArray();

        return Task.FromResult(PagedResult<DemoRequestDto>.Ok(items, totalItems, req.PageSize));
    }
}
