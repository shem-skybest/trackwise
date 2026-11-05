using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class CurrencyMapper
{
    [MapperIgnoreSource(nameof(Currency.DomainEvents))]
    [MapperIgnoreSource(nameof(Currency.Group))]
    [MapperIgnoreSource(nameof(Currency.DeletedBy))]
    [MapperIgnoreTarget(nameof(CurrencyDto.ForwardedFromId))]
    public static partial CurrencyDto ToDto(this Currency entity);
}
