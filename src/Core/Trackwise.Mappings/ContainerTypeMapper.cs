using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class ContainerTypeMapper
{
    [MapperIgnoreSource(nameof(ContainerType.DomainEvents))]
    [MapperIgnoreSource(nameof(ContainerType.Group))]
    [MapperIgnoreSource(nameof(ContainerType.DeletedBy))]
    [MapperIgnoreTarget(nameof(ContainerTypeDto.ForwardedFromId))]
    public static partial ContainerTypeDto ToDto(this ContainerType entity);
}
