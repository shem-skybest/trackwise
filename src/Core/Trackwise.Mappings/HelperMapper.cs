using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class HelperMapper
{
    [MapperIgnoreSource(nameof(Helper.DomainEvents))]
    [MapperIgnoreSource(nameof(Helper.Branch))]
    [MapperIgnoreSource(nameof(Helper.Group))]
    [MapperIgnoreSource(nameof(Helper.DeletedBy))]
    [MapperIgnoreTarget(nameof(HelperDto.BranchName))]
    [MapperIgnoreTarget(nameof(HelperDto.ForwardedFromId))]
    public static partial HelperDto ToDto(this Helper entity);

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Taking the name
    /// through a dictionary rather than <c>entity.Branch</c> is deliberate: lazy loading is on, so
    /// reading the navigation here would issue a query per row.
    /// </summary>
    public static HelperDto ToDto(this Helper entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
