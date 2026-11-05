using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Abstractions.Documents;

/// <summary>
/// One-line postal address for printed documents. Shared by the quotation and booking PDF
/// handlers so the two never format an address differently.
/// </summary>
public static class AddressFormatter
{
    public static string? ToSingleLine(Address? address)
    {
        if (address is null) return null;

        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(address.StreetAddress)) lines.Add(address.StreetAddress);
        if (!string.IsNullOrWhiteSpace(address.Barangay)) lines.Add(address.Barangay);

        var city = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.CityOrMunicipality)) city.Add(address.CityOrMunicipality);
        if (!string.IsNullOrWhiteSpace(address.Province)) city.Add(address.Province);
        if (!string.IsNullOrWhiteSpace(address.Zipcode)) city.Add(address.Zipcode);
        if (city.Count > 0) lines.Add(string.Join(" ", city));

        if (!string.IsNullOrWhiteSpace(address.Country)) lines.Add(address.Country);

        return lines.Count == 0 ? null : string.Join(", ", lines);
    }
}
