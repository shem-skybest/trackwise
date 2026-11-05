using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class MasterDataChangeLogMapper
{
    [MapperIgnoreSource(nameof(MasterDataChangeLog.DomainEvents))]
    public static partial MasterDataChangeLogDto ToDto(this MasterDataChangeLog entity);
}
