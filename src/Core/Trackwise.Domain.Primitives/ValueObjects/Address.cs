using System.ComponentModel.DataAnnotations.Schema;

namespace Trackwise.Domain.Primitives.ValueObjects;

[ComplexType]
public record Address
{
    [Column("street_address")]
    public required string StreetAddress { get; set; }

    [Column("subdivision")]
    public string? Subdivision { get; set; }

    [Column("barangay")]
    public string? Barangay { get; set; }

    [Column("city_or_municipality")]
    public required string CityOrMunicipality { get; set; }

    [Column("province")]
    public required string Province { get; set; }

    [Column("region")]
    public required string Region { get; set; }

    [Column("zipcode")]
    public required string Zipcode { get; set; }

    [Column("country")]
    public required string Country { get; set; } = "PH"; // Default to Philippines
}
