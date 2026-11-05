using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.DbMigrator.Regions;

internal sealed class USRegionProfile : IRegionProfile
{
    private readonly Random random = new();

    public Region Region => Region.US;
    public string DisplayName => "US Demo";
    public CurrencyCode Currency => CurrencyCode.USD;
    public decimal DefaultRateFloorPerMile => 2.10m;
    public string CountryCode => "US";

    public Address CompanyAddress => new()
    {
        StreetAddress = "7 Allstate Rd",
        CityOrMunicipality = "Dorchester",
        Province = "Massachusetts",
        Region = "MA",
        Zipcode = "02125",
        Country = "United States"
    };

    public IReadOnlyList<RoutePoint> RoutePoints { get; } =
    [
        new(new Address { StreetAddress = "233 S Wacker Dr", CityOrMunicipality = "Chicago", Province = "Illinois", Region = "IL", Zipcode = "60606", Country = "United States" }, -87.6298, 41.8781, "IL"),
        new(new Address { StreetAddress = "1 Monument Cir", CityOrMunicipality = "Indianapolis", Province = "Indiana", Region = "IN", Zipcode = "46204", Country = "United States" }, -86.1581, 39.7684, "IN"),
        new(new Address { StreetAddress = "100 N Capitol Ave", CityOrMunicipality = "Lansing", Province = "Michigan", Region = "MI", Zipcode = "48933", Country = "United States" }, -84.5555, 42.7325, "MI"),
        new(new Address { StreetAddress = "600 Woodward Ave", CityOrMunicipality = "Detroit", Province = "Michigan", Region = "MI", Zipcode = "48226", Country = "United States" }, -83.0458, 42.3314, "MI"),
        new(new Address { StreetAddress = "151 W Jefferson", CityOrMunicipality = "Louisville", Province = "Kentucky", Region = "KY", Zipcode = "40202", Country = "United States" }, -85.7585, 38.2527, "KY"),
        new(new Address { StreetAddress = "600 Commerce St", CityOrMunicipality = "Nashville", Province = "Tennessee", Region = "TN", Zipcode = "37203", Country = "United States" }, -86.7816, 36.1627, "TN"),
        new(new Address { StreetAddress = "1100 Congress Ave", CityOrMunicipality = "Austin", Province = "Texas", Region = "TX", Zipcode = "78701", Country = "United States" }, -97.7431, 30.2672, "TX"),
        new(new Address { StreetAddress = "55 Trinity Ave SW", CityOrMunicipality = "Atlanta", Province = "Georgia", Region = "GA", Zipcode = "30303", Country = "United States" }, -84.3902, 33.7490, "GA"),
        new(new Address { StreetAddress = "1500 Marilla St", CityOrMunicipality = "Dallas", Province = "Texas", Region = "TX", Zipcode = "75201", Country = "United States" }, -96.7970, 32.7767, "TX"),
        new(new Address { StreetAddress = "1411 Market St", CityOrMunicipality = "St. Louis", Province = "Missouri", Region = "MO", Zipcode = "63103", Country = "United States" }, -90.1994, 38.6270, "MO")
    ];

    public IReadOnlyList<TerminalSeed> Terminals { get; } =
    [
        new("Port of Los Angeles", "USLAX", "US", TerminalType.SeaPort,
            new Address { StreetAddress = "425 S Palos Verdes St", CityOrMunicipality = "San Pedro", Province = "California", Region = "CA", Zipcode = "90731", Country = "United States" }),
        new("Port of Long Beach", "USLGB", "US", TerminalType.SeaPort,
            new Address { StreetAddress = "415 W Ocean Blvd", CityOrMunicipality = "Long Beach", Province = "California", Region = "CA", Zipcode = "90802", Country = "United States" }),
        new("Port of New York / New Jersey", "USNYC", "US", TerminalType.SeaPort,
            new Address { StreetAddress = "4 World Trade Center", CityOrMunicipality = "New York", Province = "New York", Region = "NY", Zipcode = "10007", Country = "United States" }),
        new("Port of Houston", "USHOU", "US", TerminalType.SeaPort,
            new Address { StreetAddress = "111 East Loop N", CityOrMunicipality = "Houston", Province = "Texas", Region = "TX", Zipcode = "77029", Country = "United States" }),
        new("Port of Savannah", "USSAV", "US", TerminalType.SeaPort,
            new Address { StreetAddress = "2 E Bay St", CityOrMunicipality = "Savannah", Province = "Georgia", Region = "GA", Zipcode = "31401", Country = "United States" }),
        new("BNSF Chicago Trackwise Park", "USCHI", "US", TerminalType.RailTerminal,
            new Address { StreetAddress = "26664 S Baseline Rd", CityOrMunicipality = "Elwood", Province = "Illinois", Region = "IL", Zipcode ="60421", Country = "United States" }),
        new("Memphis Inland Depot", "USMEM", "US", TerminalType.InlandDepot,
            new Address { StreetAddress = "3030 Airways Blvd", CityOrMunicipality = "Memphis", Province = "Tennessee", Region = "TN", Zipcode = "38131", Country = "United States" })
    ];

    public IReadOnlyList<TruckMakeModel> FreightTruckModels { get; } =
    [
        new("Freightliner", "Cascadia", "1FU"),
        new("Peterbilt", "579", "1XP"),
        new("Kenworth", "T680", "1XK"),
        new("Volvo", "VNL 860", "4V4"),
        new("International", "LT", "3HS"),
        new("Mack", "Anthem", "1M1"),
        new("Western Star", "5700XE", "5KK")
    ];

    public IReadOnlyList<TruckMakeModel> CarHaulerModels { get; } =
    [
        new("Peterbilt", "389", "1XP"),
        new("Kenworth", "W900", "1XK"),
        new("Freightliner", "Coronado", "1FU"),
        new("Volvo", "VNL 760", "4V4"),
        new("International", "HX", "3HS")
    ];

    public IReadOnlyList<TruckMakeModel> ContainerTruckModels { get; } =
    [
        new("International", "HX", "3HS"),
        new("Kenworth", "T880", "1XK"),
        new("Peterbilt", "567", "1XP")
    ];

    public IReadOnlyList<string> TripCorridorNames { get; } =
    [
        "Midwest Sweep", "Southeast Run", "Texas Triangle", "Great Lakes Corridor",
        "I-40 Eastbound", "I-95 Northbound", "Coast-to-Coast", "Heartland Express",
        "Pacific Northwest Loop", "Rust Belt Route"
    ];

    public IReadOnlyList<string> ReeferCargoTypes { get; } =
    [
        "Frozen Meat", "Dairy Products", "Fresh Produce", "Pharmaceuticals",
        "Ice Cream", "Frozen Seafood", "Yogurt", "Frozen Pizza"
    ];

    public IReadOnlyList<string> HazMatCargoTypes { get; } =
    [
        "Industrial Solvent", "Lithium Batteries", "Compressed Gas",
        "Paint & Coatings", "Fertilizer", "Cleaning Chemicals"
    ];

    public IReadOnlyList<string> TankCargoTypes { get; } =
    [
        "Vegetable Oil", "Industrial Chemicals", "Liquid Sugar", "Crude Oil", "Diesel Fuel"
    ];

    public IReadOnlyList<VehicleMakeModel> CarHaulerCargoVehicles { get; } =
    [
        new("Toyota", "Camry"),
        new("Honda", "Civic"),
        new("Ford", "F-150"),
        new("Chevrolet", "Silverado"),
        new("Tesla", "Model 3"),
        new("Ram", "1500"),
        new("Honda", "CR-V"),
        new("Nissan", "Altima"),
        new("Jeep", "Wrangler"),
        new("BMW", "X5"),
        new("Subaru", "Outback"),
        new("Hyundai", "Sonata")
    ];

    public IReadOnlyList<string> ContainerOwnerCodes { get; } = ["MSCU", "APLU", "HLXU", "TCLU", "CMAU"];

    public IReadOnlyList<string> BrokerNames { get; } =
        ["Ridgeline Trackwise", "Cardinal Freight Group", "Blue Harbor Transport", "Sunbelt Brokerage"];

    private static readonly string[] USStates =
    [
        "TX", "CA", "FL", "GA", "IL", "OH", "PA", "NC", "MI", "NJ",
        "VA", "AZ", "TN", "IN", "MO", "WI", "CO", "AL", "SC", "LA"
    ];

    public string GenerateVin(string make)
    {
        var wmi = FreightTruckModels.Concat(CarHaulerModels).Concat(ContainerTruckModels)
            .FirstOrDefault(m => m.Make == make)?.VinWmi ?? "1XX";
        return wmi + GenerateAlphanumeric(14);
    }

    public string GenerateBrokerPhone() => $"+1 (312) 555-{random.Next(1000, 9999)}";

    public LicensePlate GeneratePlate()
    {
        var letters = GenerateLetters(3);
        var numbers = random.Next(1000, 9999);
        var state = USStates[random.Next(USStates.Length)];
        return new LicensePlate($"{letters}-{numbers}", state);
    }

    private string GenerateAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private string GenerateLetters(int length)
    {
        const string letters = "ABCDEFGHJKLMNPRSTUVWXYZ";
        return new string(Enumerable.Range(0, length).Select(_ => letters[random.Next(letters.Length)]).ToArray());
    }
}
