using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>
/// Reads a client's Cold Chain booking request - a screenshot of their Viber message or Excel
/// table, or the message text - into proposed truck-trips, matched against that client's saved
/// delivery locations. Saves nothing.
/// </summary>
public class ReadBookingRequestQuery : IQuery<Result<BookingRequestReadingDto>>
{
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The day the client sent the message, in the tenant's calendar. Resolves "bukas", "monday"
    /// and dates written without a year.
    /// </summary>
    public DateOnly ReceivedOn { get; set; }

    public byte[]? Attachment { get; set; }
    public string? AttachmentMediaType { get; set; }
    public string? AttachmentFileName { get; set; }

    /// <summary>The message text when it was copied rather than screenshotted.</summary>
    public string? MessageText { get; set; }
}
