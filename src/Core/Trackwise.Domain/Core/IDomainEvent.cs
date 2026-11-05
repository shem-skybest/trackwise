using MediatR;

namespace Trackwise.Domain.Core;

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent : INotification;
