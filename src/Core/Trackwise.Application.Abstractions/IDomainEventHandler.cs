using Trackwise.Domain.Core;
using MediatR;

namespace Trackwise.Application.Abstractions;

public interface IDomainEventHandler<in T> : INotificationHandler<T> where T : IDomainEvent;
