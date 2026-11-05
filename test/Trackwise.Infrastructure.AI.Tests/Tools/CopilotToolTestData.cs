using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.AI.Tests.Tools;

/// <summary>Shared fixtures for the copilot tool tests.</summary>
internal static class CopilotToolTestData
{
    public static Address SomeAddress => new()
    {
        StreetAddress = "123 Main St",
        CityOrMunicipality = "Dallas",
        Province = "TX",
        Region = "Texas",
        Zipcode = "75201",
        Country = "US"
    };

    public static LoadDto CreateLoad(
        Guid? id = null,
        decimal deliveryCost = 1500m,
        CustomerDto? customer = null,
        InvoiceDto? invoice = null)
    {
        return new LoadDto
        {
            Id = id ?? Guid.NewGuid(),
            Number = 42,
            Name = "Test Load",
            OriginAddress = SomeAddress,
            OriginLocation = new GeoPoint(-96.8, 32.78),
            DestinationAddress = SomeAddress,
            DestinationLocation = new GeoPoint(-95.36, 29.76),
            DeliveryCost = deliveryCost,
            Customer = customer,
            Invoice = invoice
        };
    }

    public static Money Usd(decimal amount) => new() { Amount = amount, Currency = "USD" };
}
