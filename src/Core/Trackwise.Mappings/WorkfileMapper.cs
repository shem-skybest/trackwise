using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

/// <summary>
/// Workfile read models. The customer and partner names cannot come off the entity without an
/// N+1 per row, so the generated maps leave them blank and the hand-written overloads below take
/// them as arguments - the same shape <see cref="BookingMapper"/> uses.
/// </summary>
[Mapper]
public static partial class WorkfileMapper
{
    [MapperIgnoreSource(nameof(Workfile.DomainEvents))]
    [MapperIgnoreSource(nameof(Workfile.Booking))]
    [MapperIgnoreSource(nameof(Workfile.ShippingLine))]
    [MapperIgnoreSource(nameof(Workfile.CoLoader))]
    [MapperIgnoreSource(nameof(Workfile.Forwarder))]
    [MapperIgnoreSource(nameof(Workfile.Trucker))]
    [MapperIgnoreSource(nameof(Workfile.Broker))]
    [MapperIgnoreSource(nameof(Workfile.Genset))]
    [MapperIgnoreSource(nameof(Workfile.RequiresDuplicateBlCheck))]
    [MapperIgnoreSource(nameof(Workfile.Containers))]
    [MapperIgnoreTarget(nameof(WorkfileDto.Containers))]
    [MapperIgnoreTarget(nameof(WorkfileDto.Direction))]
    [MapperIgnoreTarget(nameof(WorkfileDto.Variant))]
    [MapperIgnoreTarget(nameof(WorkfileDto.BookingNumber))]
    [MapperIgnoreTarget(nameof(WorkfileDto.CustomerName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.ShippingLineName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.CoLoaderName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.ForwarderName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.TruckerName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.BrokerName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.GensetName))]
    [MapperIgnoreTarget(nameof(WorkfileDto.ParentWorkfileId))]
    [MapperIgnoreTarget(nameof(WorkfileDto.ParentReferenceNumber))]
    [MapperIgnoreTarget(nameof(WorkfileDto.LegRole))]
    [MapperIgnoreTarget(nameof(WorkfileDto.Legs))]
    [MapperIgnoreTarget(nameof(WorkfileDto.LegLoads))]
    [MapperIgnoreTarget(nameof(WorkfileDto.Timeline))]
    private static partial WorkfileDto ToDtoCore(Workfile entity);

    [MapperIgnoreSource(nameof(Workfile.DomainEvents))]
    [MapperIgnoreSource(nameof(Workfile.Booking))]
    [MapperIgnoreSource(nameof(Workfile.ShippingLine))]
    [MapperIgnoreSource(nameof(Workfile.CoLoader))]
    [MapperIgnoreSource(nameof(Workfile.Forwarder))]
    [MapperIgnoreSource(nameof(Workfile.Trucker))]
    [MapperIgnoreSource(nameof(Workfile.Broker))]
    [MapperIgnoreSource(nameof(Workfile.Genset))]
    [MapperIgnoreSource(nameof(Workfile.Containers))]
    [MapperIgnoreTarget(nameof(WorkfileSummaryDto.CustomerName))]
    private static partial WorkfileSummaryDto ToSummaryDtoCore(Workfile entity);

    /// <param name="partnerNames">Partner id -> name, batched by the caller. A lookup per row here would be an N+1.</param>
    /// <param name="gensetName">The assigned genset's name, resolved by the caller - null when none is assigned.</param>
    /// <param name="legs">The truck legs, parent link and leg loads, batched by the caller.</param>
    public static WorkfileDto ToDto(
        this Workfile entity,
        string customerName,
        long? bookingNumber,
        IReadOnlyDictionary<Guid, string> partnerNames,
        string? gensetName,
        List<WorkfileContainerDto>? containers = null,
        WorkfileLegsView? legs = null,
        List<WorkfileTimelineEntryDto>? timeline = null)
    {
        var dto = ToDtoCore(entity);
        dto.Containers = containers ?? [];
        dto.ParentWorkfileId = (entity as TruckingWorkfile)?.ParentWorkfileId;
        dto.LegRole = (entity as TruckingWorkfile)?.LegRole;
        dto.ParentReferenceNumber = legs?.ParentReferenceNumber;
        dto.Legs = legs?.Legs ?? [];
        dto.LegLoads = legs?.LegLoads ?? [];
        dto.Timeline = timeline ?? [];
        dto.CustomerName = customerName;
        dto.BookingNumber = bookingNumber;
        dto.Direction = (entity as RegularWorkfile)?.Direction;
        dto.Variant = (entity as TruckingWorkfile)?.Variant;
        dto.ShippingLineName = NameOf(partnerNames, entity.ShippingLineId);
        dto.CoLoaderName = NameOf(partnerNames, entity.CoLoaderId);
        dto.ForwarderName = NameOf(partnerNames, entity.ForwarderId);
        dto.TruckerName = NameOf(partnerNames, entity.TruckerId);
        dto.BrokerName = NameOf(partnerNames, entity.BrokerId);
        dto.GensetName = gensetName;
        return dto;
    }

    public static WorkfileSummaryDto ToSummaryDto(this Workfile entity, string customerName)
    {
        var dto = ToSummaryDtoCore(entity);
        dto.CustomerName = customerName;
        return dto;
    }

    private static string? NameOf(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id.HasValue && names.TryGetValue(id.Value, out var name) ? name : null;
}
