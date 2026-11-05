using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripStopPhotosHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTripStopPhotosQuery, Result<List<TripStopPhotoDto>>>
{
    public async Task<Result<List<TripStopPhotoDto>>> Handle(GetTripStopPhotosQuery req, CancellationToken ct)
    {
        var stopIds = await tenantUow.Repository<TripStop>().Query()
            .Where(s => s.TripId == req.TripId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var documents = await tenantUow.Repository<DeliveryDocument>().Query()
            .Where(d => d.TripStopId.HasValue
                        && stopIds.Contains(d.TripStopId.Value)
                        && d.Status == DocumentStatus.Active
                        && d.ContentType.StartsWith("image/"))
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(ct);

        var uploaderIds = documents.Select(d => d.UploadedById).Distinct().ToList();
        var uploaders = await tenantUow.Repository<Employee>().Query()
            .IgnoreQueryFilters()
            .Where(e => uploaderIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var photos = documents
            .GroupBy(d => d.BlobPath)
            .Select(g => g.ToList().ToTripStopPhotoDto(uploaders.GetValueOrDefault(g.First().UploadedById)))
            .ToList();

        return Result<List<TripStopPhotoDto>>.Ok(photos);
    }
}
