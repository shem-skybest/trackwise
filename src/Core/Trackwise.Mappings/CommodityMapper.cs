using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class CommodityMapper
{
    [MapperIgnoreSource(nameof(Commodity.DomainEvents))]
    [MapperIgnoreSource(nameof(Commodity.Branch))]
    [MapperIgnoreSource(nameof(Commodity.Group))]
    [MapperIgnoreSource(nameof(Commodity.DeletedBy))]
    [MapperIgnoreTarget(nameof(CommodityDto.BranchName))]
    [MapperIgnoreTarget(nameof(CommodityDto.ForwardedFromId))]
    public static partial CommodityDto ToDto(this Commodity entity);

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched - never off the
    /// navigation property, which lazy loading would turn into a query per row.
    /// </summary>
    public static CommodityDto ToDto(
        this Commodity entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
