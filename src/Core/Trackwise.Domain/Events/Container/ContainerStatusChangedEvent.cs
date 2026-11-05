using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Events;

/// <summary>
/// Raised when a container transitions between lifecycle states.
/// </summary>
public record ContainerStatusChangedEvent(
    Guid ContainerId,
    string ContainerNumber,
    ContainerStatus PreviousStatus,
    ContainerStatus NewStatus) : IDomainEvent;
