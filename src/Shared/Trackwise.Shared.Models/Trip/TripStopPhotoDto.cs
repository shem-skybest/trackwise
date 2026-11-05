using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One photo from a trip's crew report - the loaded van at the warehouse, or proof at a drop. A
/// warehouse visit that picks up several loads keeps one photo for all of them, so it lists every
/// stop it covers.
/// </summary>
public record TripStopPhotoDto
{
    /// <summary>A document id for the photo - pass it to the document download or the photo delete.</summary>
    public Guid Id { get; set; }

    public List<Guid> StopIds { get; set; } = [];
    public DocumentType Type { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSizeBytes { get; set; }
    public DateTime CapturedAt { get; set; }
    public string? CapturedBy { get; set; }
}
