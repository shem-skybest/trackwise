using Trackwise.Application.Abstractions.Payments;
using Trackwise.Shared.Geo;
using Stripe;

using AddressValueObject = Trackwise.Domain.Primitives.ValueObjects.Address;
using StripeAddress = Stripe.Address;

namespace Trackwise.Infrastructure.Payments.Stripe;

internal sealed class StripeAddressMapper : IStripeAddressMapper
{
    public AddressValueObject ToAddress(StripeAddress stripeAddress)
    {
        return new AddressValueObject
        {
            CityOrMunicipality = stripeAddress.City,
            Country = stripeAddress.Country,
            StreetAddress = stripeAddress.Line1,
            Subdivision = stripeAddress.Line2,
            Zipcode = stripeAddress.PostalCode,
            Province = stripeAddress.State,
            Region = stripeAddress.State
        };
    }

    public AddressValueObject ToAddress(AddressOptions addressOptions)
    {
        return new AddressValueObject
        {
            CityOrMunicipality = addressOptions.City,
            Country = addressOptions.Country,
            StreetAddress = addressOptions.Line1,
            Subdivision = addressOptions.Line2,
            Zipcode = addressOptions.PostalCode,
            Province = addressOptions.State,
            Region = addressOptions.State
        };
    }
}

internal static class StripeAddressMapperExtensions
{
    public static AddressOptions ToStripeAddressOptions(this AddressValueObject address)
    {
        return new AddressOptions
        {
            City = address.CityOrMunicipality,
            Country = GetCountryCode(address.Country),
            Line1 = address.StreetAddress,
            Line2 = address.Subdivision,
            PostalCode = address.Zipcode,
            State = address.Province
        };
    }

    private static string? GetCountryCode(string? country)
    {
        if (string.IsNullOrEmpty(country))
        {
            return null;
        }

        if (country.Length == 2)
        {
            return country.ToUpperInvariant();
        }

        return Countries.FindCountry(country)?.Code;
    }
}
