using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class LocationMapper
{
    [MapperIgnoreSource(nameof(Location.DomainEvents))]
    [MapperIgnoreSource(nameof(Location.Branch))]
    [MapperIgnoreSource(nameof(Location.ParentLocation))]
    [MapperIgnoreSource(nameof(Location.Customer))]
    [MapperIgnoreSource(nameof(Location.Group))]
    [MapperIgnoreSource(nameof(Location.DeletedBy))]
    [MapperIgnoreTarget(nameof(LocationDto.BranchName))]
    [MapperIgnoreTarget(nameof(LocationDto.ParentLocationName))]
    [MapperIgnoreTarget(nameof(LocationDto.CustomerName))]
    [MapperIgnoreTarget(nameof(LocationDto.ForwardedFromId))]
    public static partial LocationDto ToDto(this Location entity);

    /// <summary>
    /// Adds the owning branch, parent zone and customer names from lookups the caller has already
    /// batched. All three are read through dictionaries rather than navigations: lazy loading is
    /// on, and each would otherwise be one query per row on the busiest list in the module.
    /// </summary>
    public static LocationDto ToDto(
        this Location entity,
        IReadOnlyDictionary<Guid, string> branchNames,
        IReadOnlyDictionary<Guid, string> parentNames,
        IReadOnlyDictionary<Guid, string> customerNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var branch))
        {
            dto.BranchName = branch;
        }

        if (entity.ParentLocationId is { } parentId && parentNames.TryGetValue(parentId, out var parent))
        {
            dto.ParentLocationName = parent;
        }

        if (entity.CustomerId is { } customerId && customerNames.TryGetValue(customerId, out var customer))
        {
            dto.CustomerName = customer;
        }

        return dto;
    }
}
