using Microsoft.EntityFrameworkCore;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the sample charge codes plus one versioned tariff per scenario (domestic trucking,
/// domestic reefer, import sea FCL, export air, triangular sea) with all rates attached, so a
/// Quotation has live TariffRates to price against. Container types are resolved by code from the
/// migration-seeded reference list (<c>AddContainerTypesAndCurrencies</c>) rather than re-declared here.
/// </summary>
/// <remarks>
/// Rates are persisted in the order declared in <see cref="TariffCatalog.Tariffs"/>, and
/// <see cref="TariffCatalog.Slice"/> uses that same declaration to split <c>CreatedTariffRates</c>
/// back into per-tariff lists, so QuotationSeeder never relies on magic offsets.
/// </remarks>
internal class TariffSeeder(ILogger<TariffSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(TariffSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 215;
    public override IReadOnlyList<string> DependsOn => [nameof(ChargeCodeSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Tariff>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var existingChargeCodes = context.CreatedChargeCodes;
        if (existingChargeCodes is null)
        {
            LogUpstreamSkipped();
            return;
        }

        LogStarting();

        // 1. Charge codes: reuse whatever ChargeCodeSeeder created, add any sample codes that are missing.
        var byCode = existingChargeCodes
            .GroupBy(c => c.Code)
            .ToDictionary(g => g.Key, g => g.First());

        var chargeCodeRepo = context.TenantUnitOfWork.Repository<ChargeCode>();
        foreach (var def in TariffCatalog.ChargeCodes)
        {
            if (byCode.ContainsKey(def.Code)) continue;

            var chargeCode = ChargeCode.Create(
                def.Code, def.Name, def.Category, def.Modes, def.Unit, def.RequiresApproval, def.Description);
            chargeCode.Currency = def.Currency; // Create() has no currency argument

            await chargeCodeRepo.AddAsync(chargeCode, cancellationToken);
            byCode[def.Code] = chargeCode;
        }

        // 2. Prefer a 40' High Cube for sea tariffs, fall back to the 40' GP.
        var containerTypes = await context.TenantUnitOfWork.Repository<ContainerType>()
            .Query()
            .Where(t => t.Code == "HC40" || t.Code == "GP40")
            .ToListAsync(cancellationToken);
        var container40 = containerTypes.FirstOrDefault(t => t.Code == "HC40")
                          ?? containerTypes.FirstOrDefault(t => t.Code == "GP40");

        // 3. Tariffs + rates
        var tariffRepo = context.TenantUnitOfWork.Repository<Tariff>();
        var rates = new List<TariffRate>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var def in TariffCatalog.Tariffs)
        {
            var tariff = Tariff.Create(
                def.FreightMode, ServiceLevel.Standard, def.Name, def.Currency,
                def.OriginPort, def.DestinationPort,
                def.UsesContainerType ? container40?.Id : null,
                null, today.AddMonths(-1), null);

            foreach (var r in def.Rates)
            {
                var chargeCode = byCode[r.Code];

                var rate = tariff.AddRate(
                    chargeCode.Id, r.Model, r.BaseAmount, r.Unit,
                    r.Minimum, r.Maximum, r.FreeDays, r.RateAfterFreeDays,
                    r.Percentage, r.PercentageBase, r.BreakpointJson);

                // Quotation pricing/validation reads rate.ChargeCode (category, currency), so attach it.
                rate.ChargeCode = chargeCode;
                rate.BreakpointMode = r.BreakpointMode;
                rates.Add(rate);
            }

            await tariffRepo.AddAsync(tariff, cancellationToken);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedTariffRates = rates;
        LogCompleted(rates.Count);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Seed catalog (single source of truth for TariffSeeder and QuotationSeeder)
// ─────────────────────────────────────────────────────────────────────────────

internal sealed record ChargeCodeDef(
    string Code, string Name, ChargeCategory Category, FreightMode[] Modes,
    string Unit, string Currency, bool RequiresApproval = false, string? Description = null);

internal sealed record RateDef(
    string Code, PricingModel Model, decimal BaseAmount, string Unit,
    decimal? Minimum = null, decimal? Maximum = null,
    int? FreeDays = null, decimal? RateAfterFreeDays = null,
    decimal? Percentage = null, string? PercentageBase = null,
    string? BreakpointJson = null, BreakpointMode BreakpointMode = BreakpointMode.AllUnits);

internal sealed record TariffDef(
    string Name, FreightMode FreightMode, string Currency,
    string OriginPort, string DestinationPort, bool UsesContainerType,
    IReadOnlyList<RateDef> Rates);

/// <summary>Per-tariff rate lists, split back out of <c>CreatedTariffRates</c>.</summary>
internal sealed record SeededTariffRates(
    IReadOnlyList<TariffRate> DomesticTrucking,
    IReadOnlyList<TariffRate> DomesticReefer,
    IReadOnlyList<TariffRate> ImportSeaFcl,
    IReadOnlyList<TariffRate> ExportAir,
    IReadOnlyList<TariffRate> TriangularSea,
    IReadOnlyList<TariffRate> DomesticTrucking40Ft);

internal static class SeedCodes
{
    // Domestic trucking / reefer (PHP)
    public const string TrkLinehaul = "TRK-LH";
    public const string TrkFuel = "TRK-FSC";
    public const string TrkDrop = "TRK-DROP";
    public const string TrkLiftgate = "TRK-LIFT";
    public const string TrkToll = "TRK-TOLL";
    public const string TrkDetention = "TRK-DET";
    public const string TrkDiscount = "TRK-DISC";
    public const string ReefLinehaul = "REEF-LH";
    public const string ReefPti = "REEF-PTI";
    public const string ReefGenset = "REEF-GEN";
    public const string ReefTelematics = "REEF-TEL";
    public const string ReefPlugin = "REEF-PLG";
    public const string TrkLabor = "TRK-LAB";
    public const string TrkWarehouse = "TRK-WHF";
    public const string TrkAfterHours = "TRK-OT";

    // Sea (USD)
    public const string OceanFreight40 = "OFR-40HC";
    public const string Baf = "BAF-SEA";
    public const string ThcDestination = "THC-D";
    public const string DocImport = "DOC-IMP";
    public const string DeliveryOrder = "DO-FEE";
    public const string CustomsImport = "CUS-IMP";
    public const string Demurrage = "DEM-IMP";
    public const string Insurance = "INS-CIF";

    // Air (USD)
    public const string AirFreight = "AIR-FRT";
    public const string AirFuel = "AIR-FSC";
    public const string AirSecurity = "AIR-SEC";
    public const string ThcOrigin = "THC-O";
    public const string CustomsExport = "CUS-EXP";
    public const string AwbFee = "AWB-FEE";
    public const string HazmatAir = "HAZ-AIR";

    // Triangular (USD)
    public const string TriFreight = "TRI-OFR";
    public const string TriHandling = "TRI-HDL";
    public const string BlSwitch = "BL-SWITCH";
    public const string IsfFiling = "ISF-FEE";
    public const string TriThcOrigin = "TRI-THCO";
    public const string TriDiscount = "TRI-DISC";
}

internal static class TariffCatalog
{
    private static readonly FreightMode[] Dom = [FreightMode.Domestic];
    private static readonly FreightMode[] Imp = [FreightMode.Import];
    private static readonly FreightMode[] Exp = [FreightMode.Export];
    private static readonly FreightMode[] Tri = [FreightMode.Triangular];
    private static readonly FreightMode[] Intl = [FreightMode.Import, FreightMode.Export, FreightMode.Triangular];

    public static readonly IReadOnlyList<ChargeCodeDef> ChargeCodes =
    [
        // Domestic trucking (PHP)
        new(SeedCodes.TrkLinehaul, "Trucking Linehaul",        ChargeCategory.Base,        Dom, "per trip",      "PHP"),
        new(SeedCodes.TrkFuel,     "Fuel Surcharge",           ChargeCategory.Surcharge,   Dom, "% of linehaul", "PHP"),
        new(SeedCodes.TrkDrop,     "Additional Drop Fee",      ChargeCategory.Accessorial, Dom, "per drop",      "PHP"),
        new(SeedCodes.TrkLiftgate, "Liftgate Service",         ChargeCategory.Accessorial, Dom, "per delivery",  "PHP"),
        new(SeedCodes.TrkToll,     "Toll & Port Access",       ChargeCategory.Accessorial, Dom, "per trip",      "PHP"),
        new(SeedCodes.TrkDetention,"Truck Detention",          ChargeCategory.Accessorial, Dom, "per hour",      "PHP"),
        new(SeedCodes.TrkDiscount, "Contract Volume Discount", ChargeCategory.Discount,    Dom, "per trip",      "PHP"),
        new(SeedCodes.TrkLabor,      "Loading/Unloading Labor", ChargeCategory.Accessorial, Dom, "per hour",   "PHP"),
        new(SeedCodes.TrkWarehouse,  "Warehouse Handling Fee",  ChargeCategory.Accessorial, Dom, "per pallet", "PHP"),
        new(SeedCodes.TrkAfterHours, "After-Hours Delivery",    ChargeCategory.Accessorial, Dom, "per trip",   "PHP"),

        // Domestic reefer (PHP)
        new(SeedCodes.ReefLinehaul,  "Reefer Linehaul",       ChargeCategory.Base,        Dom, "per trip", "PHP"),
        new(SeedCodes.ReefPti,       "Pre-Cooling & PTI",     ChargeCategory.Accessorial, Dom, "per trip", "PHP"),
        new(SeedCodes.ReefGenset,    "Genset Fuel",           ChargeCategory.Accessorial, Dom, "per hour", "PHP"),
        new(SeedCodes.ReefTelematics,"Continuous Telematics", ChargeCategory.Accessorial, Dom, "per trip", "PHP"),
        new(SeedCodes.ReefPlugin,    "Cold Storage Plug-in",  ChargeCategory.Accessorial, Dom, "per day",  "PHP"),

        // Import sea FCL (USD)
        new(SeedCodes.OceanFreight40, "Ocean Freight 40'HC",             ChargeCategory.Base,        Imp, "per container",      "USD"),
        new(SeedCodes.Baf,            "Bunker Adjustment Factor",        ChargeCategory.Surcharge,   Intl,"per container",      "USD"),
        new(SeedCodes.ThcDestination, "Terminal Handling - Destination", ChargeCategory.Accessorial, Imp, "per container",      "USD"),
        new(SeedCodes.DocImport,      "Import Documentation Fee",        ChargeCategory.Accessorial, Imp, "per shipment",       "USD"),
        new(SeedCodes.DeliveryOrder,  "Delivery Order Fee",              ChargeCategory.Accessorial, Imp, "per shipment",       "USD"),
        new(SeedCodes.CustomsImport,  "Customs Brokerage - Import",      ChargeCategory.Accessorial, Imp, "per entry",          "USD"),
        new(SeedCodes.Demurrage,      "Demurrage (Import)",              ChargeCategory.Accessorial, Imp, "per container/day",  "USD", Description: "Charged after free days"),
        new(SeedCodes.Insurance,      "Cargo Insurance",                 ChargeCategory.Accessorial, Intl,"% of CIF",           "USD"),

        // Export air (USD)
        new(SeedCodes.AirFreight,   "Air Freight",                 ChargeCategory.Base,        Exp, "per kg",       "USD"),
        new(SeedCodes.AirFuel,      "Fuel Surcharge (Air)",        ChargeCategory.Surcharge,   Exp, "per kg",       "USD"),
        new(SeedCodes.AirSecurity,  "Security Surcharge (Air)",    ChargeCategory.Surcharge,   Exp, "per kg",       "USD"),
        new(SeedCodes.ThcOrigin,    "Terminal Handling - Origin",  ChargeCategory.Accessorial, Exp, "per kg",       "USD"),
        new(SeedCodes.CustomsExport,"Customs Clearance - Export",  ChargeCategory.Accessorial, Exp, "per entry",    "USD"),
        new(SeedCodes.AwbFee,       "Air Waybill Fee",             ChargeCategory.Accessorial, Exp, "per shipment", "USD"),
        new(SeedCodes.HazmatAir,    "Dangerous Goods Handling",    ChargeCategory.Surcharge,   Exp, "per shipment", "USD", RequiresApproval: true),

        // Triangular (USD)
        new(SeedCodes.TriFreight,  "Ocean Freight 40'HC (Third-Country)", ChargeCategory.Base,        Tri, "per container", "USD"),
        new(SeedCodes.TriHandling, "Triangular Trade Handling Fee",       ChargeCategory.Accessorial, Tri, "per shipment",  "USD"),
        new(SeedCodes.BlSwitch,    "Switch B/L Re-issuance",              ChargeCategory.Accessorial, Tri, "per B/L",       "USD"),
        new(SeedCodes.IsfFiling,   "ISF 10+2 Filing",                     ChargeCategory.Accessorial, Tri, "per filing",    "USD"),
        new(SeedCodes.TriThcOrigin,"Terminal Handling - Origin",          ChargeCategory.Accessorial, Tri, "per container", "USD"),
        new(SeedCodes.TriDiscount, "Volume Discount",                     ChargeCategory.Discount,    Tri, "per shipment",  "USD"),
    ];

    // Chargeable-weight breakpoints (USD/kg), AllUnits mode: the matched tier prices the ENTIRE quantity.
    private const string AirBreakpoints = """
        [
          {"min":0,   "max":44,  "rate":5.20},
          {"min":45,  "max":99,  "rate":4.80},
          {"min":100, "max":299, "rate":4.30},
          {"min":300, "max":499, "rate":3.90},
          {"min":500,            "rate":3.50}
        ]
        """;

    /// <summary>ORDER MATTERS: it must match <see cref="Slice"/> and <see cref="SeededTariffRates"/>.</summary>
    public static readonly IReadOnlyList<TariffDef> Tariffs =
    [
        // 0) Domestic dry trucking: Metro Manila → Batangas (10-wheeler)
        new("Manila - Batangas Domestic Trucking 2026", FreightMode.Domestic, "PHP", "PHMNL", "PHBTG", false,
        [
            new(SeedCodes.TrkLinehaul,  PricingModel.Flat,       18_500m, "per trip"),
            new(SeedCodes.TrkFuel,      PricingModel.Percentage, 0m,      "% of linehaul", Percentage: 8m, PercentageBase: "BAS"),
            new(SeedCodes.TrkDrop,      PricingModel.PerUnit,    1_200m,  "per drop"),
            new(SeedCodes.TrkLiftgate,  PricingModel.Flat,       1_500m,  "per delivery"),
            new(SeedCodes.TrkToll,      PricingModel.Flat,       2_100m,  "per trip"),
            new(SeedCodes.TrkDetention, PricingModel.PerUnit,    350m,    "per hour", FreeDays: 4, RateAfterFreeDays: 350m),
            new(SeedCodes.TrkDiscount,  PricingModel.Flat,       500m,    "per trip"),
        ]),

        // 1) Domestic reefer: Metro Manila → Baguio
        new("Manila - Baguio Domestic Reefer 2026", FreightMode.Domestic, "PHP", "PHMNL", "PHBAG", false,
        [
            new(SeedCodes.ReefLinehaul,   PricingModel.Flat,       24_500m, "per trip"),
            new(SeedCodes.ReefPti,        PricingModel.Flat,       3_200m,  "per trip"),
            new(SeedCodes.ReefGenset,     PricingModel.PerUnit,    480m,    "per hour", Minimum: 2_400m),
            new(SeedCodes.ReefTelematics, PricingModel.Flat,       1_800m,  "per trip"),
            new(SeedCodes.ReefPlugin,     PricingModel.PerUnit,    900m,    "per day", FreeDays: 1, RateAfterFreeDays: 900m),
            new(SeedCodes.TrkToll,        PricingModel.Flat,       2_600m,  "per trip"),
            new(SeedCodes.TrkFuel,        PricingModel.Percentage, 0m,      "% of linehaul", Percentage: 10m, PercentageBase: "BAS"),
        ]),

        // 2) Import sea FCL: Shanghai → Manila
        new("China - Manila Import FCL 2026", FreightMode.Import, "USD", "CNSHA", "PHMNL", true,
        [
            new(SeedCodes.OceanFreight40, PricingModel.PerUnit,    1_850m, "per 40'HC"),
            new(SeedCodes.Baf,            PricingModel.PerUnit,    180m,   "per container"),
            new(SeedCodes.ThcDestination, PricingModel.PerUnit,    150m,   "per container"),
            new(SeedCodes.DocImport,      PricingModel.Flat,       45m,    "per shipment"),
            new(SeedCodes.DeliveryOrder,  PricingModel.Flat,       60m,    "per shipment"),
            new(SeedCodes.CustomsImport,  PricingModel.Flat,       150m,   "per entry", Minimum: 150m),
            new(SeedCodes.Demurrage,      PricingModel.PerUnit,    75m,    "per container/day", FreeDays: 7, RateAfterFreeDays: 75m),
            new(SeedCodes.Insurance,      PricingModel.Percentage, 0m,     "% of CIF", Minimum: 25m, Percentage: 0.15m, PercentageBase: "CIF"),
        ]),

        // 3) Export air: Manila (NAIA) → Los Angeles (LAX)
        new("Manila - Los Angeles Export Air 2026", FreightMode.Export, "USD", "PHMNL", "USLAX", false,
        [
            new(SeedCodes.AirFreight,    PricingModel.Breakpoint, 0m,    "per kg", Minimum: 85m, BreakpointJson: AirBreakpoints, BreakpointMode: BreakpointMode.AllUnits),
            new(SeedCodes.AirFuel,       PricingModel.PerUnit,    0.45m, "per kg"),
            new(SeedCodes.AirSecurity,   PricingModel.PerUnit,    0.12m, "per kg", Minimum: 15m),
            new(SeedCodes.ThcOrigin,     PricingModel.PerUnit,    0.08m, "per kg", Minimum: 25m),
            new(SeedCodes.CustomsExport, PricingModel.Flat,       95m,   "per entry"),
            new(SeedCodes.AwbFee,        PricingModel.Flat,       35m,   "per shipment"),
            new(SeedCodes.HazmatAir,     PricingModel.Flat,       120m,  "per shipment"),
            new(SeedCodes.Insurance,     PricingModel.Percentage, 0m,    "% of CIF", Minimum: 25m, Percentage: 0.20m, PercentageBase: "CIF"),
        ]),

        // 4) Triangular sea: Shenzhen (shipper) → Long Beach (consignee), handled via Manila
        new("Shenzhen - Long Beach Triangular FCL (via Manila) 2026", FreightMode.Triangular, "USD", "CNSZX", "USLGB", true,
        [
            new(SeedCodes.TriFreight,   PricingModel.PerUnit, 3_200m, "per 40'HC"),
            new(SeedCodes.Baf,          PricingModel.PerUnit, 220m,   "per container"),
            new(SeedCodes.TriThcOrigin, PricingModel.PerUnit, 140m,   "per container"),
            new(SeedCodes.TriHandling,  PricingModel.Flat,    250m,   "per shipment"),
            new(SeedCodes.BlSwitch,     PricingModel.Flat,    75m,    "per B/L"),
            new(SeedCodes.IsfFiling,    PricingModel.Flat,    60m,    "per filing"),
            new(SeedCodes.TriDiscount,  PricingModel.Flat,    100m,   "per shipment"),
        ]),

        new("Metro Manila - Batangas Port 40'FT Domestic Trucking 2026", FreightMode.Domestic, "PHP", "PHMNL", "PHBTG", false,
        [
            new(SeedCodes.TrkLinehaul,   PricingModel.Flat,       8_500m, "per trip"),
            new(SeedCodes.TrkFuel,       PricingModel.Percentage, 0m,     "% of TRK", Percentage: 8m, PercentageBase: "BAS"),
            new(SeedCodes.TrkToll,       PricingModel.Flat,       650m,   "per trip"),
            new(SeedCodes.TrkLabor,      PricingModel.PerUnit,    150m,   "per hour"),
            new(SeedCodes.TrkWarehouse,  PricingModel.PerUnit,    45m,    "per pallet"),
            new(SeedCodes.TrkAfterHours, PricingModel.Flat,       2_000m, "per trip"),
        ]),
    ];

    /// <summary>Splits the flat <c>CreatedTariffRates</c> list back into per-tariff lists.</summary>
    public static SeededTariffRates Slice(IEnumerable<TariffRate> allRates)
    {
        var all = allRates.ToList();
        var expected = Tariffs.Sum(t => t.Rates.Count);
        if (all.Count != expected)
            throw new InvalidOperationException(
                $"Expected {expected} seeded tariff rates but found {all.Count}; TariffSeeder and TariffCatalog are out of sync.");

        var offset = 0;
        IReadOnlyList<TariffRate> Next(int index)
        {
            var slice = all.Skip(offset).Take(Tariffs[index].Rates.Count).ToList();
            offset += slice.Count;
            return slice;
        }

        return new SeededTariffRates(Next(0), Next(1), Next(2), Next(3), Next(4), Next(5));
    }

    /// <summary>Finds a rate by charge code inside one tariff's rate list (codes are unique per tariff).</summary>
    public static TariffRate Rate(this IReadOnlyList<TariffRate> tariffRates, string code) =>
        tariffRates.SingleOrDefault(r => r.ChargeCode?.Code == code)
        ?? throw new InvalidOperationException($"Seeded tariff has no rate for charge code '{code}'.");
}
