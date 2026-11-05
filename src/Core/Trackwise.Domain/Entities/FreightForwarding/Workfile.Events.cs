using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

public record WorkfileCreated(Guid Id, string ReferenceNumber, Guid? BookingId) : IDomainEvent;
public record WorkfileSubmittedForApproval(Guid Id, string ReferenceNumber) : IDomainEvent;
public record WorkfileReturnedToOps(Guid Id, string Reason) : IDomainEvent;
public record WorkfileForwardedToAccounting(Guid Id, string ReferenceNumber) : IDomainEvent;
public record WorkfileClosed(Guid Id, string ClosedBy) : IDomainEvent;
public record WorkfileCancelled(Guid Id, string CancelledBy, string Reason) : IDomainEvent;
public record WorkfileReferenceLocked(Guid Id, string ReferenceNumber) : IDomainEvent;
public record WorkfileReferenceReleased(Guid Id, string ReferenceNumber) : IDomainEvent;
public record WorkfileGarageDepartureRecorded(Guid Id, DateTimeOffset At) : IDomainEvent;
public record WorkfileGarageArrivalRecorded(Guid Id, DateTimeOffset At) : IDomainEvent;
public record TruckLegAdded(Guid ParentWorkfileId, Guid LegId, string ReferenceNumber, TruckLegRole Role) : IDomainEvent;
