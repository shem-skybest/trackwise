using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;

internal static class TruckstopMapper
{
    public static LoadBoardListingDto ToListingDto(TruckstopLoad load)
    {
        return new LoadBoardListingDto
        {
            ExternalListingId = load.LoadId ?? string.Empty,
            ProviderType = LoadBoardProviderType.Truckstop,
            ProviderName = "Truckstop",
            OriginAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = load.Origin?.City ?? "Unknown",
                    Province = load.Origin?.StateProvince ?? "Unknown",
                    Region = "",
                    Zipcode = load.Origin?.PostalCode ?? "00000",
                    Country = "US"
                },
            OriginLocation = new GeoPoint(load.Origin?.Longitude ?? 0, load.Origin?.Latitude ?? 0),
            DestinationAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = load.Destination?.City ?? "Unknown",
                    Province = load.Destination?.StateProvince ?? "Unknown",
                    Region = "",
                    Zipcode = load.Destination?.PostalCode ?? "00000",
                    Country = "US"
                },
            DestinationLocation = new GeoPoint(load.Destination?.Longitude ?? 0, load.Destination?.Latitude ?? 0),
            RatePerMile = load.RatePerMile,
            TotalRate = load.Rate,
            Currency = "USD",
            Distance = load.Miles,
            Weight = load.Weight,
            Length = load.Length,
            PickupDateStart = load.PickupDate,
            PickupDateEnd = load.PickupDateEnd,
            DeliveryDateStart = load.DeliveryDate,
            DeliveryDateEnd = load.DeliveryDateEnd,
            EquipmentType = load.EquipmentType,
            Commodity = load.Commodity,
            BrokerName = load.Broker?.CompanyName,
            BrokerPhone = load.Broker?.Phone,
            BrokerEmail = load.Broker?.Email,
            BrokerMcNumber = load.Broker?.McNumber,
            BrokerCreditScore = load.Broker?.CreditScore,
            BrokerDaysToPay = load.Broker?.DaysToPay,
            Status = LoadBoardListingStatus.Available,
            ExpiresAt = load.ExpiresAt ?? DateTime.MaxValue
        };
    }

    public static PostedTruckDto ToPostedTruckDto(TruckstopTruck truck)
    {
        return new PostedTruckDto
        {
            ExternalPostId = truck.TruckId,
            ProviderType = LoadBoardProviderType.Truckstop,
            ProviderName = "Truckstop",
            AvailableAtAddress =
                new Address
                {
                    StreetAddress = "N/A",
                    CityOrMunicipality = truck.Origin?.City ?? "Unknown",
                    Province = truck.Origin?.StateProvince ?? "Unknown",
                    Region = "",
                    Zipcode = truck.Origin?.PostalCode ?? "00000",
                    Country = "US"
                },
            AvailableAtLocation = new GeoPoint(truck.Origin?.Longitude ?? 0, truck.Origin?.Latitude ?? 0),
            EquipmentType = truck.EquipmentType,
            AvailableFrom = truck.AvailableDate ?? DateTime.UtcNow,
            AvailableTo = truck.AvailableDateEnd,
            ExpiresAt = truck.ExpiresAt
        };
    }

    public static LoadBoardWebhookEventType MapWebhookEventType(string? eventType)
    {
        return eventType?.ToLowerInvariant() switch
        {
            "load_posted" => LoadBoardWebhookEventType.LoadPosted,
            "load_cancelled" => LoadBoardWebhookEventType.LoadCancelled,
            "load_expired" => LoadBoardWebhookEventType.LoadExpired,
            "rate_updated" or "rate_changed" => LoadBoardWebhookEventType.RateUpdated,
            "status_changed" => LoadBoardWebhookEventType.StatusChanged,
            "truck_expired" => LoadBoardWebhookEventType.TruckPostExpired,
            _ => LoadBoardWebhookEventType.Unknown
        };
    }
}
