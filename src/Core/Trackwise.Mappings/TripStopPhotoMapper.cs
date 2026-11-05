using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

/// <summary>
/// Folds the documents that share one stored photo - one per stop of a warehouse visit - back into
/// the single photo the crew report shows.
/// </summary>
public static class TripStopPhotoMapper
{
    public static TripStopPhotoDto ToTripStopPhotoDto(
        this IReadOnlyCollection<DeliveryDocument> sameBlob, string? capturedBy)
    {
        var first = sameBlob.First();
        return new TripStopPhotoDto
        {
            Id = first.Id,
            StopIds = sameBlob.Where(d => d.TripStopId.HasValue).Select(d => d.TripStopId!.Value).ToList(),
            Type = first.Type,
            FileName = first.OriginalFileName,
            ContentType = first.ContentType,
            FileSizeBytes = first.FileSizeBytes,
            CapturedAt = first.CapturedAt ?? first.CreatedAt,
            CapturedBy = capturedBy
        };
    }
}
