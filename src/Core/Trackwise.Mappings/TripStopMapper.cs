using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class TripStopMapper
{
    [MapperIgnoreSource(nameof(Trip.DomainEvents))]
    public static partial TripStopDto ToDto(this TripStop entity);
}
