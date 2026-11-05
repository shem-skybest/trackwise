using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Events;

/// <summary>
/// Raised when a load's proximity status changes (can confirm pickup/delivery).
/// Contains all data needed for notifications to avoid additional queries.
/// </summary>
public record LoadProximityChangedEvent(
    Guid LoadId,
    long LoadNumber,
    LoadStatus StatusToConfirm,
    Guid TruckId,
    string TruckNumber,
    string? MainDriverDeviceToken,
    string? SecondaryDriverDeviceToken) : IDomainEvent;
