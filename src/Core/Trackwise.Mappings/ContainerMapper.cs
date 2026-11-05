using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class ContainerMapper
{
    [MapperIgnoreSource(nameof(Container.CurrentTerminal))]
    [MapperIgnoreSource(nameof(Container.ContainerType))]
    [MapperIgnoreSource(nameof(Container.DomainEvents))]
    [MapperIgnoreTarget(nameof(ContainerDto.Shipments))]
    private static partial ContainerDto ToDtoCore(this Container entity);

    /// <summary>
    /// Terminal display values are passed in rather than read off <c>entity.CurrentTerminal</c>.
    /// Lazy loading made that nav a query per row, and now that terminals are soft-deletable it
    /// also returns null for a retired port - blanking the terminal on containers still sitting
    /// there. Callers batch the lookup past the query filter and hand the result over.
    /// </summary>
    public static ContainerDto ToDto(
        this Container entity,
        TerminalRef? terminal = null,
        ContainerTypeRef? containerType = null)
    {
        var dto = entity.ToDtoCore();

        if (terminal is { } value)
        {
            dto.CurrentTerminalName = value.Name;
            dto.CurrentTerminalCode = value.Code;
        }

        // Same treatment, and now mandatory rather than merely cheaper: the type used to be an
        // enum that rendered itself, and a foreign key does not.
        if (containerType is { } type)
        {
            dto.ContainerTypeCode = type.Code;
            dto.ContainerTypeName = type.Name;
        }

        return dto;
    }
}
