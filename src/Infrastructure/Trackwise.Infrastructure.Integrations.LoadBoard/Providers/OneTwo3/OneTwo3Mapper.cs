using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Integrations.LoadBoard.Providers.OneTwo3;

internal static class OneTwo3Mapper
{
    public static LoadBoardListingDto ToListingDto(OneTwo3Load load)
    {
        return new LoadBoardListingDto
        {
            ExternalListingId = load.Id ?? string.Empty,
            ProviderType = LoadBoardProviderType.OneTwo3Loadboard,
            ProviderName = "123Loadboard",
            OriginAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = load.Origin?.City ?? "Unknown",
                    Province = load.Origin?.State ?? "Unknown",
                    Region = "",
                    Zipcode = load.Origin?.ZipCode ?? "00000",
                    Country = "US"
                },
            OriginLocation = new GeoPoint(load.Origin?.Lng ?? 0, load.Origin?.Lat ?? 0),
            DestinationAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = load.Destination?.City ?? "Unknown",
                    Province = load.Destination?.State ?? "Unknown",
                    Region = "",
                    Zipcode = load.Destination?.ZipCode ?? "00000",
                    Country = "US"
                },
            DestinationLocation = new GeoPoint(load.Destination?.Lng ?? 0, load.Destination?.Lat ?? 0),
            RatePerMile = load.RatePerMile,
            TotalRate = load.TotalRate,
            Currency = "USD",
            Distance = load.Miles,
            Weight = load.Weight,
            Length = load.Length,
            PickupDateStart = load.PickupStart,
            PickupDateEnd = load.PickupEnd,
            DeliveryDateStart = load.DeliveryStart,
            DeliveryDateEnd = load.DeliveryEnd,
            EquipmentType = load.Equipment,
            Commodity = load.Commodity,
            BrokerName = load.Poster?.CompanyName,
            BrokerPhone = load.Poster?.Phone,
            BrokerEmail = load.Poster?.Email,
            BrokerMcNumber = load.Poster?.McNumber,
            Status = LoadBoardListingStatus.Available,
            ExpiresAt = load.ExpiresAt ?? DateTime.MaxValue
        };
    }

    public static PostedTruckDto ToPostedTruckDto(OneTwo3Truck truck)
    {
        return new PostedTruckDto
        {
            ExternalPostId = truck.Id,
            ProviderType = LoadBoardProviderType.OneTwo3Loadboard,
            ProviderName = "123Loadboard",
            AvailableAtAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = truck.Origin?.City ?? "Unknown",
                    Province = truck.Origin?.State ?? "Unknown",
                    Region = "",
                    Zipcode = truck.Origin?.ZipCode ?? "00000",
                    Country = "US"
                },
            AvailableAtLocation = new GeoPoint(truck.Origin?.Lng ?? 0, truck.Origin?.Lat ?? 0),
            EquipmentType = truck.Equipment,
            AvailableFrom = truck.AvailableFrom ?? DateTime.UtcNow,
            AvailableTo = truck.AvailableTo,
            ExpiresAt = truck.ExpiresAt
        };
    }

    public static LoadBoardWebhookEventType MapWebhookEventType(string? eventType)
    {
        return eventType?.ToLowerInvariant() switch
        {
            "load.posted" => LoadBoardWebhookEventType.LoadPosted,
            "load.removed" or "load.cancelled" => LoadBoardWebhookEventType.LoadCancelled,
            "load.expired" => LoadBoardWebhookEventType.LoadExpired,
            "rate.updated" => LoadBoardWebhookEventType.RateUpdated,
            "status.changed" => LoadBoardWebhookEventType.StatusChanged,
            "truck.expired" => LoadBoardWebhookEventType.TruckPostExpired,
            _ => LoadBoardWebhookEventType.Unknown
        };
    }
}
