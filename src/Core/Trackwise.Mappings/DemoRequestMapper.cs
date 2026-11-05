using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class DemoRequestMapper
{
    [MapperIgnoreSource(nameof(DemoRequest.DomainEvents))]
    public static partial DemoRequestDto ToDto(this DemoRequest entity);
}
