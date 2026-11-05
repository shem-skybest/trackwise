using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Platform.Portal.Queries;

internal sealed class GetPortalLoadDocumentsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetPortalLoadDocumentsQuery, Result<IEnumerable<DocumentDto>>>
{
    public async Task<Result<IEnumerable<DocumentDto>>> Handle(
        GetPortalLoadDocumentsQuery req,
        CancellationToken ct)
    {
        // Verify the load belongs to the customer
        var loadExists = await tenantUow.Repository<Load>().Query()
            .AnyAsync(l => l.Id == req.LoadId && l.CustomerId == req.CustomerId, ct);

        if (!loadExists)
        {
            return Result<IEnumerable<DocumentDto>>.Fail("Load not found or access denied.");
        }

        var documents = await tenantUow.Repository<LoadDocument>().Query()
            .Where(d => d.LoadId == req.LoadId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var dtos = documents.Select(d => d.ToDto());
        return Result<IEnumerable<DocumentDto>>.Ok(dtos);
    }
}
