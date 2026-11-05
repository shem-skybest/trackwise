using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.DbMigrator.Regions;

internal sealed class PHRegionProfile : IRegionProfile
{
    private readonly Random random = new();

    public Region Region => Region.PH;
    public string DisplayName => "PH Demo";
    public CurrencyCode Currency => CurrencyCode.PHP;
    public decimal DefaultRateFloorPerMile => 45.00m;
    public string CountryCode => "PH";

    public Address CompanyAddress => new()
    {
        StreetAddress = "6788 Ayala Avenue",
        CityOrMunicipality = "Makati City",
        Province = "Metro Manila",
        Region = "NC",
        Zipcode = "1226",
        Country = "Philippines"
    };

    public IReadOnlyList<RoutePoint> RoutePoints { get; } =
    [
        new(new Address { StreetAddress = "Port Area", CityOrMunicipality = "Manila", Province = "Metro Manila", Region = "NC", Zipcode = "1018", Country = "Philippines" }, 120.9628, 14.5906, "NC"),
        new(new Address { StreetAddress = "Quezon Ave", CityOrMunicipality = "Quezon City", Province = "Metro Manila", Region = "NC", Zipcode = "1100", Country = "Philippines" }, 121.0509, 14.6760, "QC"),
        new(new Address { StreetAddress = "M.J. Cuenco Ave", CityOrMunicipality = "Cebu City", Province = "Cebu", Region = "CB", Zipcode = "6000", Country = "Philippines" }, 123.8854, 10.3157, "CB"),
        new(new Address { StreetAddress = "R. Castillo St", CityOrMunicipality = "Davao City", Province = "Davao del Sur", Region = "DV", Zipcode = "8000", Country = "Philippines" }, 125.6128, 7.0731, "DV"),
        new(new Address { StreetAddress = "Corrales Ave", CityOrMunicipality = "Cagayan de Oro", Province = "Misamis Oriental", Region = "CG", Zipcode = "9000", Country = "Philippines" }, 124.6319, 8.4822, "CG"),
        new(new Address { StreetAddress = "Iznart St", CityOrMunicipality = "Iloilo City", Province = "Iloilo", Region = "IL", Zipcode = "5000", Country = "Philippines" }, 122.5621, 10.6979, "IL"),
        new(new Address { StreetAddress = "Kumintang Ibaba", CityOrMunicipality = "Batangas City", Province = "Batangas", Region = "BT", Zipcode = "4200", Country = "Philippines" }, 121.0583, 13.7565, "BT"),
        new(new Address { StreetAddress = "Rizal Highway", CityOrMunicipality = "Olongapo", Province = "Zambales", Region = "ZM", Zipcode = "2200", Country = "Philippines" }, 120.2830, 14.8294, "ZM"),
        new(new Address { StreetAddress = "Pioneer Ave", CityOrMunicipality = "General Santos", Province = "South Cotabato", Region = "GS", Zipcode = "9500", Country = "Philippines" }, 125.1716, 6.1128, "GS"),
        new(new Address { StreetAddress = "Session Rd", CityOrMunicipality = "Baguio", Province = "Benguet", Region = "BG", Zipcode = "2600", Country = "Philippines" }, 120.5960, 16.4023, "BG")
    ];

    public IReadOnlyList<TerminalSeed> Terminals { get; } =
    [
        new("Manila International Container Terminal", "PHMNL", "PH", TerminalType.SeaPort,
            new Address { StreetAddress = "Manila North Harbor", CityOrMunicipality = "Manila", Province = "Metro Manila", Region = "NC", Zipcode = "1012", Country = "Philippines" }),
        new("Port of Cebu", "PHCEB", "PH", TerminalType.SeaPort,
            new Address { StreetAddress = "Osmeña Blvd", CityOrMunicipality = "Cebu City", Province = "Cebu", Region = "CB", Zipcode = "6000", Country = "Philippines" }),
        new("Davao Sasa Wharf", "PHDVO", "PH", TerminalType.SeaPort,
            new Address { StreetAddress = "Sasa", CityOrMunicipality = "Davao City", Province = "Davao del Sur", Region = "DV", Zipcode = "8000", Country = "Philippines" }),
        new("Batangas International Port", "PHBTG", "PH", TerminalType.SeaPort,
            new Address { StreetAddress = "Sta. Clara", CityOrMunicipality = "Batangas City", Province = "Batangas", Region = "BT", Zipcode = "4200", Country = "Philippines" }),
        new("Subic Bay Freeport", "PHSFS", "PH", TerminalType.SeaPort,
            new Address { StreetAddress = "Alava Wharf", CityOrMunicipality = "Subic Bay Freeport Zone", Province = "Zambales", Region = "ZM", Zipcode = "2222", Country = "Philippines" }),
        new("Clark Freeport Logistics Hub", "PHCRK", "PH", TerminalType.InlandDepot,
            new Address { StreetAddress = "Manuel A. Roxas Hwy", CityOrMunicipality = "Clark Freeport Zone", Province = "Pampanga", Region = "CL", Zipcode = "2023", Country = "Philippines" })
    ];

    public IReadOnlyList<TruckMakeModel> FreightTruckModels { get; } =
    [
        new("Isuzu", "Giga", "JAA"),
        new("Hino", "700 Series", "JHD"),
        new("Fuso", "Super Great", "JL6"),
        new("UD Trucks", "Quon", "JMY"),
        new("Hyundai", "Xcient", "KMF")
    ];

    public IReadOnlyList<TruckMakeModel> CarHaulerModels { get; } =
    [
        new("Isuzu", "Forward", "JAA"),
        new("Hino", "500 Series", "JHD"),
        new("Fuso", "Fighter", "JL6")
    ];

    public IReadOnlyList<TruckMakeModel> ContainerTruckModels { get; } =
    [
        new("Isuzu", "Forward", "JAA"),
        new("Hino", "500 Series", "JHD"),
        new("Fuso", "Fighter", "JL6")
    ];

    public IReadOnlyList<string> TripCorridorNames { get; } =
    [
        "NLEX-SLEX Backbone", "Pan-Philippine Highway", "Nautical Highway", "Cebu-Davao Route",
        "Bicol Corridor", "Mindanao Loop", "Visayas Island-Hopper", "Clark-Subic Corridor",
        "Metro Manila Circumferential", "CALABARZON Sweep"
    ];

    public IReadOnlyList<string> ReeferCargoTypes { get; } =
    [
        "Fresh Seafood", "Frozen Seafood", "Fresh Mangoes", "Fresh Bananas",
        "Dairy Products", "Frozen Meat", "Pharmaceuticals", "Ice Cream"
    ];

    public IReadOnlyList<string> HazMatCargoTypes { get; } =
    [
        "LPG Cylinders", "Industrial Solvent", "Lithium Batteries",
        "Compressed Gas", "Fertilizer", "Cleaning Chemicals"
    ];

    public IReadOnlyList<string> TankCargoTypes { get; } =
    [
        "Coconut Oil", "Molasses", "Diesel Fuel", "Industrial Chemicals", "Crude Oil"
    ];

    public IReadOnlyList<VehicleMakeModel> CarHaulerCargoVehicles { get; } =
    [
        new("Toyota", "Vios"),
        new("Toyota", "Innova"),
        new("Toyota", "Hilux"),
        new("Toyota", "Fortuner"),
        new("Mitsubishi", "Montero Sport"),
        new("Ford", "Ranger"),
        new("Honda", "City"),
        new("Nissan", "Navara"),
        new("Suzuki", "Ertiga"),
        new("Isuzu", "D-Max")
    ];

    public IReadOnlyList<string> ContainerOwnerCodes { get; } = ["EISU", "OOLU", "COSU", "APLU", "MAEU"];

    public IReadOnlyList<string> BrokerNames { get; } =
        ["Luzon Freight Partners", "Visayas Cargo Solutions", "Mindanao Freight Hub", "Manila Bay Brokerage"];

    public string GenerateVin(string make)
    {
        var wmi = FreightTruckModels.Concat(CarHaulerModels).Concat(ContainerTruckModels)
            .FirstOrDefault(m => m.Make == make)?.VinWmi ?? "JXX";
        return wmi + GenerateAlphanumeric(14);
    }

    public string GenerateBrokerPhone() => $"+63 9{random.Next(10, 99)} {random.Next(100, 999)} {random.Next(1000, 9999)}";

    public LicensePlate GeneratePlate()
    {
        var letters = GenerateLetters(3);
        var numbers = random.Next(1000, 9999);
        return new LicensePlate($"{letters} {numbers}", "PH");
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
