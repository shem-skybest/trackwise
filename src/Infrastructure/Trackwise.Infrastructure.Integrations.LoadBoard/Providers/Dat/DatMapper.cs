using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Dat;

internal static class DatMapper
{
    public static LoadBoardListingDto ToListingDto(DatLoad load)
    {
        return new LoadBoardListingDto
        {
            ExternalListingId = load.Id ?? string.Empty,
            ProviderType = LoadBoardProviderType.Dat,
            ProviderName = "DAT",
            OriginAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = load.Origin?.City ?? "Unknown",
                    Province = load.Origin?.State ?? "Unknown",
                    Region = "",
                    Zipcode = "00000",
                    Country = "US"
                },
            OriginLocation = new GeoPoint(load.Origin?.Longitude ?? 0, load.Origin?.Latitude ?? 0),
            DestinationAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = load.Destination?.City ?? "Unknown",
                    Province = load.Destination?.State ?? "Unknown",
                    Region = "",
                    Zipcode = "00000",
                    Country = "US"
                },
            DestinationLocation = new GeoPoint(load.Destination?.Longitude ?? 0, load.Destination?.Latitude ?? 0),
            RatePerMile = load.RatePerMile,
            TotalRate = load.TotalRate,
            Currency = "USD",
            Distance = load.Distance,
            Weight = load.Weight,
            Length = load.Length,
            PickupDateStart = load.PickupDateStart,
            PickupDateEnd = load.PickupDateEnd,
            DeliveryDateStart = load.DeliveryDateStart,
            DeliveryDateEnd = load.DeliveryDateEnd,
            EquipmentType = load.EquipmentType,
            Commodity = load.Commodity,
            BrokerName = load.Broker?.Name,
            BrokerPhone = load.Broker?.Phone,
            BrokerEmail = load.Broker?.Email,
            BrokerMcNumber = load.Broker?.McNumber,
            BrokerCreditScore = load.Broker?.CreditScore,
            BrokerDaysToPay = load.Broker?.DaysToPay,
            Status = LoadBoardListingStatus.Available,
            ExpiresAt = load.ExpiresAt ?? DateTime.MaxValue
        };
    }

    public static PostedTruckDto ToPostedTruckDto(DatTruck truck)
    {
        return new PostedTruckDto
        {
            ExternalPostId = truck.Id,
            ProviderType = LoadBoardProviderType.Dat,
            ProviderName = "DAT",
            AvailableAtAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = truck.Origin?.City ?? "Unknown",
                    Province = truck.Origin?.State ?? "Unknown",
                    Region = "",
                    Zipcode = "00000",
                    Country = "US"
                },
            AvailableAtLocation = new GeoPoint(truck.Origin?.Longitude ?? 0, truck.Origin?.Latitude ?? 0),
            EquipmentType = truck.EquipmentType,
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
            "load.cancelled" => LoadBoardWebhookEventType.LoadCancelled,
            "load.expired" => LoadBoardWebhookEventType.LoadExpired,
            "rate.changed" or "rate.updated" => LoadBoardWebhookEventType.RateUpdated,
            "status.changed" => LoadBoardWebhookEventType.StatusChanged,
            "truck.expired" => LoadBoardWebhookEventType.TruckPostExpired,
            _ => LoadBoardWebhookEventType.Unknown
        };
    }
}
