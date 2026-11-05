using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class BusinessPartnerMapper
{
    [MapperIgnoreSource(nameof(BusinessPartner.DomainEvents))]
    [MapperIgnoreSource(nameof(BusinessPartner.Customer))]
    [MapperIgnoreSource(nameof(BusinessPartner.Branch))]
    [MapperIgnoreSource(nameof(BusinessPartner.Group))]
    [MapperIgnoreSource(nameof(BusinessPartner.DeletedBy))]
    [MapperIgnoreTarget(nameof(BusinessPartnerDto.BranchName))]
    [MapperIgnoreTarget(nameof(BusinessPartnerDto.ForwardedFromId))]
    [MapperIgnoreTarget(nameof(BusinessPartnerDto.SubmittedByName))]
    [MapperIgnoreTarget(nameof(BusinessPartnerDto.SubmittedAt))]
    [MapperIgnoreTarget(nameof(BusinessPartnerDto.LastReviewNote))]
    [MapperIgnoreTarget(nameof(BusinessPartnerDto.State))]
    private static partial BusinessPartnerDto Map(BusinessPartner entity);

    /// <summary>
    /// The state is derived here rather than mapped, for the reason it is derived everywhere:
    /// <c>IsActive</c>, <c>DeletedAt</c>, <c>MergedIntoId</c> and <c>ApprovalState</c> stay the
    /// single source of truth, and a status the client assembled itself would eventually assemble
    /// it differently from the server.
    /// </summary>
    public static BusinessPartnerDto ToDto(this BusinessPartner entity)
    {
        var dto = Map(entity);

        dto.State = MasterDataStates.Derive(
            entity.IsActive, entity.DeletedAt, entity.MergedIntoId, entity.ApprovalState);

        return dto;
    }

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Taking the name
    /// through a dictionary rather than <c>entity.Branch</c> is deliberate: lazy loading is on, so
    /// reading the navigation here would issue a query per row.
    /// </summary>
    public static BusinessPartnerDto ToDto(
        this BusinessPartner entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
