using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class TerminalMapper
{
    [MapperIgnoreSource(nameof(Terminal.DomainEvents))]
    [MapperIgnoreSource(nameof(Terminal.Branch))]
    [MapperIgnoreSource(nameof(Terminal.Group))]
    [MapperIgnoreSource(nameof(Terminal.DeletedBy))]
    [MapperIgnoreTarget(nameof(TerminalDto.BranchName))]
    [MapperIgnoreTarget(nameof(TerminalDto.ForwardedFromId))]
    public static partial TerminalDto ToDto(this Terminal entity);

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Taking the name
    /// through a dictionary rather than <c>entity.Branch</c> is deliberate: lazy loading is on, so
    /// reading the navigation here would issue a query per row.
    /// </summary>
    public static TerminalDto ToDto(
        this Terminal entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
