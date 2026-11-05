using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class TripMapper
{
    [MapperIgnoreSource(nameof(Trip.DomainEvents))]
    [MapperIgnoreSource(nameof(Trip.Truck))]
    [MapperIgnoreSource(nameof(Trip.Helper))]
    [MapperIgnoreSource(nameof(Trip.SecondHelper))]
    [MapperIgnoreSource(nameof(Trip.Driver))]
    [MapperIgnoreSource(nameof(Trip.PriceOutdatedSince))]
    [MapperIgnoreSource(nameof(Trip.Rescues))]
    [MapperIgnoreTarget(nameof(TripDto.Rescues))]
    [MapperIgnoreTarget(nameof(TripDto.TruckNumber))]
    [MapperIgnoreTarget(nameof(TripDto.HelperName))]
    [MapperIgnoreTarget(nameof(TripDto.SecondHelperName))]
    [MapperIgnoreTarget(nameof(TripDto.DriverName))]
    [MapProperty(nameof(Trip.Stops), nameof(TripDto.Stops))]
    [MapProperty(nameof(Trip.CreatedAt), nameof(TripDto.CreatedAt))]
    [MapPropertyFromSource(nameof(TripDto.OriginAddress), Use = nameof(MapOriginAddress))]
    [MapPropertyFromSource(nameof(TripDto.DestinationAddress), Use = nameof(MapDestinationAddress))]
    [MapPropertyFromSource(nameof(TripDto.Loads), Use = nameof(MapLoads))]
    [MapPropertyFromSource(nameof(TripDto.LoadsCount), Use = nameof(MapLoadsCount))]
    [MapPropertyFromSource(nameof(TripDto.TotalRevenue), Use = nameof(MapTotalRevenue))]
    [MapPropertyFromSource(nameof(TripDto.TotalPlannedBoxes), Use = nameof(MapTotalPlannedBoxes))]
    [MapPropertyFromSource(nameof(TripDto.TotalPlannedBags), Use = nameof(MapTotalPlannedBags))]
    public static partial TripDto ToDto(this Trip entity);

    /// <summary>
    /// A stop's cut-off is its load's requested delivery time. Reading <c>Load</c> here is not an
    /// extra query per stop: the trip's loads are the stops' loads, already read for
    /// <see cref="MapLoads" />.
    /// </summary>
    [MapperIgnoreSource(nameof(TripStop.DomainEvents))]
    [MapperIgnoreSource(nameof(TripStop.Trip))]
    [MapperIgnoreSource(nameof(TripStop.Load))]
    [MapProperty(nameof(@TripStop.Load.RequestedDeliveryDate), nameof(TripStopDto.CutOff))]
    private static partial TripStopDto MapStop(TripStop stop);

    /// <summary>
    /// Adds the truck number and crew names from lookups the caller has already batched. Taking them
    /// this way rather than through the navigations is deliberate - lazy loading is on, so reading a
    /// navigation here would issue a query per row, and trucks, employees and helpers are
    /// soft-deletable, so a navigation would read null once the record has been retired.
    /// </summary>
    public static TripDto ToDto(this Trip entity, TripRefNames names)
    {
        var dto = entity.ToDto();
        return dto with
        {
            TruckNumber = names.TruckNumber,
            DriverName = names.DriverName,
            HelperName = names.HelperName,
            SecondHelperName = names.SecondHelperName
        };
    }

    private static IEnumerable<TripLoadDto> MapLoads(Trip trip)
    {
        return trip.GetLoads().Select(LoadMapper.ToTripLoadDto);
    }

    private static Address MapOriginAddress(Trip trip)
    {
        return trip.GetOriginAddress();
    }

    private static Address MapDestinationAddress(Trip trip)
    {
        return trip.GetDestinationAddress();
    }

    private static int MapLoadsCount(Trip trip)
    {
        return trip.GetLoads().Count();
    }

    private static decimal MapTotalRevenue(Trip trip)
    {
        return trip.CalcTotalRevenue();
    }

    private static int MapTotalPlannedBoxes(Trip trip)
    {
        return trip.GetLoads().Sum(l => l.PlannedBoxes ?? 0);
    }

    private static int MapTotalPlannedBags(Trip trip)
    {
        return trip.GetLoads().Sum(l => l.PlannedBags ?? 0);
    }
}
