using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class GensetMapper
{
    [MapperIgnoreSource(nameof(Genset.DomainEvents))]
    [MapperIgnoreSource(nameof(Genset.Branch))]
    [MapperIgnoreSource(nameof(Genset.Group))]
    [MapperIgnoreSource(nameof(Genset.DeletedBy))]
    [MapperIgnoreTarget(nameof(GensetDto.BranchName))]
    [MapperIgnoreTarget(nameof(GensetDto.ForwardedFromId))]
    public static partial GensetDto ToDto(this Genset entity);

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Taking the name
    /// through a dictionary rather than <c>entity.Branch</c> is deliberate: lazy loading is on, so
    /// reading the navigation here would issue a query per row.
    /// </summary>
    public static GensetDto ToDto(this Genset entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
