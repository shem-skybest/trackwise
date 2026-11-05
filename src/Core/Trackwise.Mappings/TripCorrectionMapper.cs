using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class TripCorrectionMapper
{
    [MapperIgnoreSource(nameof(TripCorrection.DomainEvents))]
    [MapperIgnoreSource(nameof(TripCorrection.TripId))]
    public static partial TripCorrectionDto ToDto(this TripCorrection entity);
}
