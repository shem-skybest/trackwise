using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Removes a crew-report photo from every stop it covers - a warehouse visit's photo goes from all
/// of the visit's loads at once - and deletes the stored file.
/// </summary>
public class DeleteTripStopPhotoCommand : ICommand
{
    public Guid TripId { get; set; }

    /// <summary>Any one of the photo's document ids, as <c>TripStopPhotoDto.Id</c> gives it.</summary>
    public Guid PhotoId { get; set; }
}
