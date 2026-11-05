using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class VesselMapper
{
    [MapperIgnoreSource(nameof(Vessel.DomainEvents))]
    [MapperIgnoreSource(nameof(Vessel.Branch))]
    [MapperIgnoreSource(nameof(Vessel.Group))]
    [MapperIgnoreSource(nameof(Vessel.DeletedBy))]
    [MapperIgnoreTarget(nameof(VesselDto.BranchName))]
    [MapperIgnoreTarget(nameof(VesselDto.ForwardedFromId))]
    public static partial VesselDto ToDto(this Vessel entity);

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Taking the name
    /// through a dictionary rather than <c>entity.Branch</c> is deliberate: lazy loading is on, so
    /// reading the navigation here would issue a query per row.
    /// </summary>
    public static VesselDto ToDto(this Vessel entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
