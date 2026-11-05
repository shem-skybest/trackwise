using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.DbMigrator.Regions;

internal sealed class EURegionProfile : IRegionProfile
{
    private readonly Random random = new();

    public Region Region => Region.EU;
    public string DisplayName => "EU Demo";
    public CurrencyCode Currency => CurrencyCode.EUR;
    public decimal DefaultRateFloorPerMile => 1.35m;
    public string CountryCode => "NL";

    public Address CompanyAddress => new()
    {
        StreetAddress = "1 Company St",
        CityOrMunicipality = "Amsterdam",
        Province = "North Holland",
        Region = "NH",
        Zipcode = "1011 AC",
        Country = "Netherlands"
    };

    public IReadOnlyList<RoutePoint> RoutePoints { get; } =
    [
        new(new Address { StreetAddress = "Wilhelminakade 909", CityOrMunicipality = "Rotterdam", Province = "South Holland", Region = "ZH", Zipcode = "3072 AP", Country = "Netherlands" }, 4.4818, 51.9067, "ZH"),
        new(new Address { StreetAddress = "Brouwersvliet 33", CityOrMunicipality = "Antwerp", Province = "Antwerp", Region = "VAN", Zipcode = "2000", Country = "Netherlands" }, 4.4025, 51.2194, "VAN"),
        new(new Address { StreetAddress = "Bei St. Annen 1", CityOrMunicipality = "Hamburg", Province = "Hamburg", Region = "HH", Zipcode = "20457", Country = "Germany" }, 9.9937, 53.5511, "HH"),
        new(new Address { StreetAddress = "Schifferstraße 24", CityOrMunicipality = "Duisburg", Province = "North Rhine-Westphalia", Region = "NW", Zipcode = "47059", Country = "Germany" }, 6.7623, 51.4344, "NW"),
        new(new Address { StreetAddress = "1 Pl. Bellecour", CityOrMunicipality = "Lyon", Province = "Auvergne-Rhône-Alpes", Region = "ARA", Zipcode = "69002", Country = "France" }, 4.8357, 45.7640, "ARA"),
        new(new Address { StreetAddress = "Via Manzoni 12", CityOrMunicipality = "Milano", Province = "Lombardy", Region = "LOM", Zipcode = "20121", Country = "Italy" }, 9.1895, 45.4642, "LOM"),
        new(new Address { StreetAddress = "Carrer de Mallorca 401", CityOrMunicipality = "Barcelona", Province = "Catalonia", Region = "CT", Zipcode = "08013", Country = "Spain" }, 2.1734, 41.3851, "CT"),
        new(new Address { StreetAddress = "Plac Defilad 1", CityOrMunicipality = "Warszawa", Province = "Masovian", Region = "MZ", Zipcode = "00-901", Country = "Poland" }, 21.0118, 52.2297, "MZ"),
        new(new Address { StreetAddress = "Marienplatz 1", CityOrMunicipality = "München", Province = "Bavaria", Region = "BY", Zipcode = "80331", Country = "Germany" }, 11.5755, 48.1374, "BY"),
        new(new Address { StreetAddress = "Place Charles de Gaulle", CityOrMunicipality = "Paris", Province = "Île-de-France", Region = "IDF", Zipcode = "75008", Country = "France" }, 2.3522, 48.8566, "IDF")
    ];

    public IReadOnlyList<TerminalSeed> Terminals { get; } =
    [
        new("Port of Antwerp-Bruges", "BEANR", "BE", TerminalType.SeaPort,
            new Address { StreetAddress = "Zaha Hadidplein 1", CityOrMunicipality = "Antwerp", Province = "Antwerp", Region = "VAN", Zipcode = "2030", Country = "Belgium" }),
        new("Port of Rotterdam", "NLRTM", "NL", TerminalType.SeaPort,
            new Address { StreetAddress = "Wilhelminakade 909", CityOrMunicipality = "Rotterdam", Province = "South Holland", Region = "ZH", Zipcode = "3072 AP", Country = "Netherlands" }),
        new("Port of Hamburg", "DEHAM", "DE", TerminalType.SeaPort,
            new Address { StreetAddress = "Bei St. Annen 1", CityOrMunicipality = "Hamburg", Province = "Hamburg", Region = "HH", Zipcode = "20457", Country = "Germany" }),
        new("Port of Bremerhaven", "DEBRV", "DE", TerminalType.SeaPort,
            new Address { StreetAddress = "Senator-Borttscheller-Straße 1", CityOrMunicipality = "Bremerhaven", Province = "Bremen", Region = "HB", Zipcode = "27568", Country = "Germany" }),
        new("Duisburger Hafen Intermodal", "DEDUI", "DE", TerminalType.RailTerminal,
            new Address { StreetAddress = "Alte Ruhrorter Str. 42-52", CityOrMunicipality = "Duisburg", Province = "North Rhine-Westphalia", Region = "NW", Zipcode = "47119", Country = "Germany" }),
        new("Lyon Inland Depot", "FRLYS", "FR", TerminalType.InlandDepot,
            new Address { StreetAddress = "Rue de la Tour", CityOrMunicipality = "Lyon", Province = "Auvergne-Rhône-Alpes", Region = "ARA", Zipcode = "69007", Country = "France" }),
        new("Port of Le Havre", "FRLEH", "FR", TerminalType.SeaPort,
            new Address { StreetAddress = "Terre-Plein de la Barre", CityOrMunicipality = "Le Havre", Province = "Normandy", Region = "NOR", Zipcode = "76600", Country = "France" })
    ];

    public IReadOnlyList<TruckMakeModel> FreightTruckModels { get; } =
    [
        new("Scania", "R 500", "YS2"),
        new("MAN", "TGX 18.510", "WMA"),
        new("DAF", "XF 480", "XLR"),
        new("Iveco", "S-Way", "ZCF"),
        new("Volvo", "FH16", "YV2"),
        new("Mercedes-Benz", "Actros 1851", "WDB"),
        new("Renault", "T High", "VF6")
    ];

    public IReadOnlyList<TruckMakeModel> CarHaulerModels { get; } =
    [
        new("Scania", "R 450", "YS2"),
        new("MAN", "TGS 26.440", "WMA"),
        new("Volvo", "FH 460", "YV2"),
        new("Mercedes-Benz", "Actros 2545", "WDB"),
        new("DAF", "XF 450 FAN", "XLR")
    ];

    public IReadOnlyList<TruckMakeModel> ContainerTruckModels { get; } =
    [
        new("Scania", "G 410", "YS2"),
        new("MAN", "TGX 18.470 4x2", "WMA"),
        new("Volvo", "FH 500", "YV2"),
        new("DAF", "XF 480 FT", "XLR")
    ];

    public IReadOnlyList<string> TripCorridorNames { get; } =
    [
        "Rhine Corridor", "Mediterranean Run", "Benelux Loop", "Iberian Sweep",
        "Alpine Crossing", "Baltic Route", "Hanseatic Express", "Po Valley Run",
        "Donau Eastbound", "Channel Crossing"
    ];

    public IReadOnlyList<string> ReeferCargoTypes { get; } =
    [
        "Dairy Products", "Frozen Meat", "Fresh Produce", "Pharmaceuticals",
        "Fresh Seafood", "Cheese & Yogurt", "Frozen Bakery", "Wine"
    ];

    public IReadOnlyList<string> HazMatCargoTypes { get; } =
    [
        "ADR Class 3 Flammables", "Compressed Gas", "Lithium Batteries",
        "Industrial Chemicals", "Fertilizer", "Paint & Coatings"
    ];

    public IReadOnlyList<string> TankCargoTypes { get; } =
    [
        "Olive Oil", "Industrial Chemicals", "Bulk Wine", "Liquid Sugar", "Diesel Fuel"
    ];

    public IReadOnlyList<VehicleMakeModel> CarHaulerCargoVehicles { get; } =
    [
        new("Volkswagen", "Golf"),
        new("Renault", "Clio"),
        new("Peugeot", "308"),
        new("Škoda", "Octavia"),
        new("BMW", "3 Series"),
        new("Mercedes-Benz", "C-Class"),
        new("Fiat", "500"),
        new("Audi", "A4"),
        new("Opel", "Astra"),
        new("Toyota", "Yaris"),
        new("SEAT", "Leon"),
        new("Volvo", "XC60")
    ];

    public IReadOnlyList<string> ContainerOwnerCodes { get; } = ["MAEU", "CMAU", "HLBU", "MSCU", "MSKU"];

    public IReadOnlyList<string> BrokerNames { get; } =
        ["Vandermeer Expeditie", "Rhein-Cargo Spedition", "Adriatica Trasporti", "Nord Freight Partners"];

    // Country code → weight (more weight = more frequent in plate generation).
    // Roughly mirrors EU truck-registration share: DE/PL/NL/IT/FR/ES/BE largest.
    private static readonly (string Country, int Weight)[] PlateCountries =
    [
        ("DE", 25), ("PL", 18), ("NL", 12), ("IT", 10),
        ("FR", 10), ("ES", 8), ("BE", 7), ("AT", 5), ("CZ", 5)
    ];

    private static readonly int TotalPlateWeight = PlateCountries.Sum(p => p.Weight);

    public string GenerateVin(string make)
    {
        var wmi = FreightTruckModels.Concat(CarHaulerModels).Concat(ContainerTruckModels)
            .FirstOrDefault(m => m.Make == make)?.VinWmi ?? "WXX";
        return wmi + GenerateAlphanumeric(14);
    }

    public string GenerateBrokerPhone() => $"+31 10 555 {random.Next(1000, 9999)}";

    public LicensePlate GeneratePlate()
    {
        var country = PickWeightedCountry();
        var number = country switch
        {
            "DE" => $"{GenerateLetters(random.Next(1, 4))}-{GenerateLetters(random.Next(1, 3))} {random.Next(1, 9999)}",
            "NL" => $"{random.Next(10, 100):D2}-{GenerateLetters(3)}-{random.Next(0, 10)}",
            "FR" => $"{GenerateLetters(2)}-{random.Next(100, 1000):D3}-{GenerateLetters(2)}",
            "BE" => $"{random.Next(1, 10)}-{GenerateLetters(3)}-{random.Next(100, 1000):D3}",
            "IT" => $"{GenerateLetters(2)}{random.Next(100, 1000):D3}{GenerateLetters(2)}",
            "ES" => $"{random.Next(1000, 10000):D4} {GenerateLetters(3)}",
            "PL" => $"{GenerateLetters(2)} {GenerateAlphanumeric(5)}",
            "AT" => $"W-{GenerateAlphanumeric(5)}",
            "CZ" => $"{random.Next(1, 10)}{GenerateLetters(2)} {random.Next(1000, 10000):D4}",
            _ => $"{GenerateLetters(3)}-{random.Next(1000, 9999)}"
        };
        return new LicensePlate(number, country);
    }

    private string PickWeightedCountry()
    {
        var roll = random.Next(TotalPlateWeight);
        var cumulative = 0;
        foreach (var (country, weight) in PlateCountries)
        {
            cumulative += weight;
            if (roll < cumulative)
            {
                return country;
            }
        }
        return PlateCountries[0].Country;
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
