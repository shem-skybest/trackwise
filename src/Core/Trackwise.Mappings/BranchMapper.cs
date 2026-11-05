using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class BranchMapper
{
    [MapperIgnoreSource(nameof(Branch.DomainEvents))]
    [MapperIgnoreSource(nameof(Branch.DeletedBy))]
    [MapperIgnoreSource(nameof(Branch.Group))]
    // Set by the read handler when a merged id forwarded here; there is no entity field for it.
    [MapperIgnoreTarget(nameof(BranchDto.ForwardedFromId))]
    public static partial BranchDto ToDto(this Branch entity);
}
