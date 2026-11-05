using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

/// <summary>
/// The type code and box number are passed in: reading them off navigations would be two lazy
/// loads per row.
/// </summary>
[Mapper]
public static partial class WorkfileContainerMapper
{
    [MapperIgnoreTarget(nameof(WorkfileContainerDto.ContainerTypeCode))]
    [MapperIgnoreTarget(nameof(WorkfileContainerDto.ContainerTypeName))]
    [MapperIgnoreTarget(nameof(WorkfileContainerDto.ContainerNumber))]
    [MapperIgnoreTarget(nameof(WorkfileContainerDto.MoveStatus))]
    [MapperIgnoreTarget(nameof(WorkfileContainerDto.MoveStatusPlace))]
    [MapperIgnoreTarget(nameof(WorkfileContainerDto.MoveStatusSince))]
    [MapperIgnoreSource(nameof(WorkfileContainer.WorkfileId))]
    private static partial WorkfileContainerDto ToDtoCore(WorkfileContainer entity);

    public static WorkfileContainerDto ToDto(
        this WorkfileContainer entity, ContainerTypeRef? containerType, string? containerNumber)
    {
        var dto = ToDtoCore(entity);
        dto.ContainerTypeCode = containerType?.Code;
        dto.ContainerTypeName = containerType?.Name;
        dto.ContainerNumber = containerNumber;
        return dto;
    }
}
