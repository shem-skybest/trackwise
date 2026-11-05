using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds quotations across the multi-mode freight forwarding and cold-chain lifecycle
/// (Draft, PendingApproval, Approved, Issued, Accepted, Rejected) covering domestic trucking,
/// domestic reefer, import sea FCL, export air and triangular sea, so downstream
/// BookingSeeder and WorkfileSeeder have operational files to promote.
/// </summary>
internal class QuotationSeeder(ILogger<QuotationSeeder> logger) : SeederBase(logger)
{
    /// <summary>
    /// Stand-in for the application-layer FX service. Quotation needs a USD equivalent of the cargo value
    /// for its high-value approval rule, so PHP cargo can never be mistaken for dollars.
    /// </summary>
    //private const decimal PhpPerUsd = 58m;

    //private static decimal ToUsd(decimal php) => Math.Round(php / PhpPerUsd, 2);

    public override string Name => nameof(QuotationSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 225;

    public override IReadOnlyList<string> DependsOn =>
        [nameof(CustomerSeeder), nameof(TariffSeeder), nameof(LocationSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Quotation>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var customers = context.CreatedCustomers;
        var allRates = context.CreatedTariffRates;

        if (customers is null || allRates is null)
        {
            LogUpstreamSkipped();
            return;
        }

        if (customers.Count == 0 || allRates.Count == 0)
        {
            context.CreatedQuotations = [];
            LogCompleted(0);
            return;
        }

        // 1. Resolve seed locations for Origin/Destination stops
        var locationRepo = context.TenantUnitOfWork.Repository<Location>();
        var locations = context.CreatedLocations;

        if (locations is null || locations.Count < 2)
        {
            locations = await locationRepo.Query()
                .Where(l => l.Type != LocationType.Zone && l.IsActive && l.DeletedAt == null)
                .Take(6)
                .ToListAsync(cancellationToken);
        }

        if (locations.Count < 2)
        {
            logger.LogWarning("Insufficient physical Location master data found to generate Quotation transit routes.");
            LogUpstreamSkipped();
            return;
        }

        LogStarting();

        var now = DateTimeOffset.UtcNow;
        var origin = locations[0];
        var dest = locations[1];
        var intermediateStop = locations.Count > 2 ? locations[2] : locations[1];

        // Rates are split per tariff using the same catalog TariffSeeder wrote them from.
        var tariffs = TariffCatalog.Slice(allRates);

        // Spread quotations over the available customers instead of hard-failing on small customer lists.
        Guid Customer(int index) => customers[index % customers.Count].Id;

        var repo = context.TenantUnitOfWork.Repository<Quotation>();
        var created = new List<Quotation>();

        async Task Save(Quotation quotation)
        {
            await repo.AddAsync(quotation, cancellationToken);
            created.Add(quotation);
        }

        // =========================================================================
        // 1. Ocean Import FCL (Shanghai -> Manila, 2x40'HC) -> Approved
        //    (Consumed by BookingSeeder for AccountingReview workfile flow)
        // =========================================================================
        var importLane = LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(2), now.AddDays(16), 2850m);

        var importQuotation = Quotation.Create(
            customerId: Customer(0),
            referenceNumber: "QT-2026-IMP-001",
            currency: "USD",
            mode: TransportMode.Ocean,
            freightMode: FreightMode.Import,
            shipmentType: ShipmentType.FCL,
            incoterm: Incoterm.FOB,
            lane: importLane,
            cargoSpec: CargoSpecification.Create(
                "Consumer Electronics & Components", 18500m, 40,
                new Money { Amount = 145000m, Currency = "USD" }),
            requiresCustomsBrokerage: true,
            notes: "China import, 2x40'HC");

        AddLines(importQuotation, tariffs.ImportSeaFcl,
            Line(SeedCodes.OceanFreight40, OperationalServiceType.LineHaul, 2, MarkupType.Percentage, 15m),
            Line(SeedCodes.Baf, OperationalServiceType.BunkerAdjustmentFactor, 2, MarkupType.Percentage, 15m),
            Line(SeedCodes.ThcDestination, OperationalServiceType.TerminalHandlingDestination, 2, MarkupType.Percentage, 15m),
            Line(SeedCodes.DocImport, OperationalServiceType.Standard, 1, MarkupType.Flat, 10m),
            Line(SeedCodes.DeliveryOrder, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m),
            Line(SeedCodes.CustomsImport, OperationalServiceType.CustomsClearanceImport, 1, MarkupType.Percentage, 20m),
            Line(SeedCodes.Insurance, OperationalServiceType.Standard, 1, MarkupType.Percentage, 10m, "0.15% of CIF, min $25"));

        importQuotation.Approve("Seed Data");
        await Save(importQuotation);

        // =========================================================================
        // 2. Second Ocean Import FCL (1x40'HC) -> Approved
        //    (Consumed by BookingSeeder for Closed workfile flow)
        // =========================================================================
        var secondImportQuotation = Quotation.Create(
            customerId: Customer(0),
            referenceNumber: "QT-2026-IMP-002",
            currency: "USD",
            mode: TransportMode.Ocean,
            freightMode: FreightMode.Import,
            shipmentType: ShipmentType.FCL,
            incoterm: Incoterm.CIF,
            lane: importLane,
            cargoSpec: CargoSpecification.Create(
                "Textiles & Garments", 9200m, 20,
                new Money { Amount = 65000m, Currency = "USD" }),
            requiresCustomsBrokerage: true,
            notes: "China import, 1x40'HC - second shipment");

        AddLines(secondImportQuotation, tariffs.ImportSeaFcl,
            Line(SeedCodes.OceanFreight40, OperationalServiceType.LineHaul, 1, MarkupType.Percentage, 15m),
            Line(SeedCodes.Baf, OperationalServiceType.BunkerAdjustmentFactor, 1, MarkupType.Percentage, 15m),
            Line(SeedCodes.ThcDestination, OperationalServiceType.TerminalHandlingDestination, 1, MarkupType.Percentage, 15m),
            Line(SeedCodes.DocImport, OperationalServiceType.Standard, 1, MarkupType.Flat, 10m),
            Line(SeedCodes.CustomsImport, OperationalServiceType.CustomsClearanceImport, 1, MarkupType.Percentage, 20m),
            Line(SeedCodes.Demurrage, OperationalServiceType.DemurrageDetention, 10, MarkupType.Flat, 0m, "Estimate: 10 days dwell (7 free)"));

        secondImportQuotation.Approve("Seed Data");
        await Save(secondImportQuotation);

        // =========================================================================
        // 3. Air Export (Manila NAIA -> Los Angeles LAX) -> Issued (prices snapshotted)
        //    Breakpoint (all-units) air freight + CIF-based insurance percentage.
        // =========================================================================
        var exportLane = LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(5), now.AddDays(7), 11700m);

        var exportQuotation = Quotation.Create(
            customerId: Customer(1),
            referenceNumber: "QT-2026-EXP-001",
            currency: "USD",
            mode: TransportMode.Air,
            freightMode: FreightMode.Export,
            shipmentType: ShipmentType.AirExpress,
            incoterm: Incoterm.CIF,
            lane: exportLane,
            cargoSpec: CargoSpecification.Create(
                "Semiconductor Components", 1250m, 6,
                new Money { Amount = 45000m, Currency = "USD" }),
            requiresCustomsBrokerage: true,
            notes: "Air export, chargeable weight 1,250 kg");

        const decimal chargeableKg = 1250m;
        AddLines(exportQuotation, tariffs.ExportAir,
            Line(SeedCodes.AirFreight, OperationalServiceType.LineHaul, chargeableKg, MarkupType.Percentage, 8m),
            Line(SeedCodes.AirFuel, OperationalServiceType.Standard, chargeableKg, MarkupType.Percentage, 8m),
            Line(SeedCodes.AirSecurity, OperationalServiceType.Standard, chargeableKg, MarkupType.Percentage, 8m),
            Line(SeedCodes.ThcOrigin, OperationalServiceType.TerminalHandlingOrigin, chargeableKg, MarkupType.Percentage, 10m),
            Line(SeedCodes.CustomsExport, OperationalServiceType.CustomsClearanceExport, 1, MarkupType.Percentage, 20m),
            Line(SeedCodes.AwbFee, OperationalServiceType.Standard, 1, MarkupType.Flat, 5m),
            Line(SeedCodes.Insurance, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "0.20% of CIF, min $25"));

        exportQuotation.Approve("Seed Data");
        exportQuotation.Issue(TimeSpan.FromDays(14)); // resolves CIF base from cargo value, then snapshots prices
        await Save(exportQuotation);

        // =========================================================================
        // 4. Triangular Sea (Shenzhen -> Long Beach, handled via Manila) -> Accepted
        //    Full lifecycle: Approve -> Issue -> Accept. Includes a Discount line.
        // =========================================================================
        var triangularLane = LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(6), now.AddDays(24), 11300m);

        var triangularQuotation = Quotation.Create(
            customerId: Customer(1),
            referenceNumber: "QT-2026-TRI-001",
            currency: "USD",
            mode: TransportMode.Ocean,
            freightMode: FreightMode.Triangular,
            shipmentType: ShipmentType.FCL,
            incoterm: Incoterm.CIF,
            lane: triangularLane,
            cargoSpec: CargoSpecification.Create(
                "Furniture", 16500m, 0,
                new Money { Amount = 62000m, Currency = "USD" }),
            notes: "Shenzhen -> Long Beach, switch B/L issued in Manila");

        AddLines(triangularQuotation, tariffs.TriangularSea,
            Line(SeedCodes.TriFreight, OperationalServiceType.LineHaul, 1, MarkupType.Flat, 120m),
            Line(SeedCodes.Baf, OperationalServiceType.BunkerAdjustmentFactor, 1, MarkupType.Percentage, 0m),
            Line(SeedCodes.TriThcOrigin, OperationalServiceType.TerminalHandlingOrigin, 1, MarkupType.Percentage, 10m),
            Line(SeedCodes.TriHandling, OperationalServiceType.Standard, 1, MarkupType.Percentage, 0m),
            Line(SeedCodes.BlSwitch, OperationalServiceType.Standard, 1, MarkupType.Flat, 25m),
            Line(SeedCodes.IsfFiling, OperationalServiceType.SecurityFilingISF, 1, MarkupType.Flat, 15m),
            Line(SeedCodes.TriDiscount, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "Volume incentive"));

        triangularQuotation.Approve("Seed Data");
        triangularQuotation.Issue(TimeSpan.FromDays(14));
        triangularQuotation.Accept();
        await Save(triangularQuotation);

        // =========================================================================
        // 5. Cold Chain Biopharma Multi-Drop (Manila -> Baguio) -> PendingApproval
        //    PHP 38M cargo is ~USD 655k, which trips the $500k high-value rule.
        // =========================================================================
        var multiDropStops = new List<RouteStop>
        {
            RouteStop.Create(1, origin.Id, StopType.Pickup, now.AddDays(1), now.AddDays(1).AddHours(3), "Depot Pre-Cooled Reefer Loading"),
            RouteStop.Create(2, intermediateStop.Id, StopType.Dropoff, now.AddDays(1).AddHours(6), now.AddDays(1).AddHours(8), "Intermediate Hospital DC Drop"),
            RouteStop.Create(3, dest.Id, StopType.Dropoff, now.AddDays(2), now.AddDays(2).AddHours(4), "Main Distribution Hub")
        };

        var vaccineValuePhp = 38_000_000m;

        var coldChainQuotation = Quotation.Create(
            customerId: Customer(2),
            referenceNumber: "QT-2026-COLD-001",
            currency: "PHP",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: new LaneTransit(multiDropStops, 250m),
            cargoSpec: CargoSpecification.Create(
                "Clinical Vaccines & Injectables", 3200m, 8,
                new Money { Amount = vaccineValuePhp, Currency = "PHP" },
                requiresActiveRefrigeration: true),
            tempSpec: TemperatureControl.Create(TemperatureZone.Chilled, 4.0m, 1.5m, ComplianceRegime.GDP, requiresTelematics: true),
            notes: "Multi-drop GDP vaccine run (Manila -> Baguio)",
            declaredCargoValuePhp: vaccineValuePhp);

        AddReeferLines(coldChainQuotation, tariffs.DomesticReefer, markup: 20m);

        coldChainQuotation.Submit(); // -> PendingApproval (high-value cargo)
        await Save(coldChainQuotation);

        // =========================================================================
        // 6. Standard Domestic Reefer (Manila -> Baguio) -> Approved (auto-approved on Submit)
        // =========================================================================
        var reeferValuePhp = 420_000m;

        var reeferQuotation = Quotation.Create(
            customerId: Customer(2),
            referenceNumber: "QT-2026-COLD-002",
            currency: "PHP",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(2).AddHours(5), now.AddDays(2).AddHours(12), 250m),
            cargoSpec: CargoSpecification.Create(
                "Chilled Dairy & Vaccines", 3200m, 10,
                new Money { Amount = reeferValuePhp, Currency = "PHP" },
                requiresActiveRefrigeration: true),
            tempSpec: TemperatureControl.Create(TemperatureZone.Chilled, 4m, 2m, ComplianceRegime.GDP, requiresTelematics: true),
            notes: "GDP-compliant reefer, 2-8C",
            declaredCargoValuePhp: reeferValuePhp);

        AddReeferLines(reeferQuotation, tariffs.DomesticReefer, markup: 15m);

        reeferQuotation.Submit(); // standard value -> auto-approved by SYSTEM_AUTO_APPROVER
        await Save(reeferQuotation);

        // =========================================================================
        // 7. Domestic Road Quotation (Manila -> Batangas) -> Rejected
        // =========================================================================
        const decimal rejectedValuePhp = 15_000m;

        var rejectedQuotation = Quotation.Create(
            customerId: Customer(3),
            referenceNumber: "QT-2026-DOM-REJ",
            currency: "PHP",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(1), now.AddDays(2), 120m),
            cargoSpec: CargoSpecification.Create(
                "Dry Packaged Foodstuff", 5000m, 10,
                new Money { Amount = rejectedValuePhp, Currency = "PHP" }),
            notes: "Manila-Batangas, expired rate card",
            declaredCargoValuePhp: rejectedValuePhp);

        AddLines(rejectedQuotation, tariffs.DomesticTrucking,
            Line(SeedCodes.TrkLinehaul, OperationalServiceType.LineHaul, 1, MarkupType.Percentage, 12m),
            Line(SeedCodes.TrkFuel, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "8% of linehaul"),
            Line(SeedCodes.TrkToll, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "Pass-through"));

        rejectedQuotation.Reject("Seed Data", "Rate card expired - resubmit with current tariff.");
        await Save(rejectedQuotation);

        // =========================================================================
        // 8. Domestic Road Quotation (full trucking charge set) -> Draft
        // =========================================================================
        const decimal draftValuePhp = 380_000m;

        var draftQuotation = Quotation.Create(
            customerId: Customer(4),
            referenceNumber: "QT-2026-DOM-DFT",
            currency: "PHP",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(3), now.AddDays(3).AddHours(9), 115m),
            cargoSpec: CargoSpecification.Create(
                "Packaged Beverages", 9500m, 18,
                new Money { Amount = draftValuePhp, Currency = "PHP" }),
            notes: "Daily dedicated 10-wheeler, port delivery - pending review",
            declaredCargoValuePhp: draftValuePhp);

        AddLines(draftQuotation, tariffs.DomesticTrucking,
            Line(SeedCodes.TrkLinehaul, OperationalServiceType.LineHaul, 1, MarkupType.Percentage, 12m),
            Line(SeedCodes.TrkFuel, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "8% of linehaul"),
            Line(SeedCodes.TrkToll, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "Pass-through"),
            Line(SeedCodes.TrkDrop, OperationalServiceType.Standard, 2, MarkupType.Percentage, 10m, "2 additional drops"),
            Line(SeedCodes.TrkLiftgate, OperationalServiceType.LiftgatePickupDelivery, 1, MarkupType.Percentage, 10m),
            Line(SeedCodes.TrkDiscount, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "Contract rebate"));

        await Save(draftQuotation); // stays Draft

        // =========================================================================
        // 9. Cancelled Lane Quotation (Manila -> Batangas) -> Approved
        //    (Used by BookingSeeder to test cancellation flows)
        // =========================================================================
        const decimal cancelledValuePhp = 18_000m;

        var cancelledLaneQuotation = Quotation.Create(
            customerId: Customer(5),
            referenceNumber: "QT-2026-DOM-CANC",
            currency: "PHP",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: LaneTransit.PointToPoint(origin.Id, dest.Id, now.AddDays(4), now.AddDays(5), 110m),
            cargoSpec: CargoSpecification.Create(
                "Office Furniture", 2200m, 6,
                new Money { Amount = cancelledValuePhp, Currency = "PHP" }),
            notes: "Manila-Batangas, customer cancelled",
            declaredCargoValuePhp: cancelledValuePhp);

        AddLines(cancelledLaneQuotation, tariffs.DomesticTrucking,
            Line(SeedCodes.TrkLinehaul, OperationalServiceType.LineHaul, 1, MarkupType.Percentage, 12m),
            Line(SeedCodes.TrkFuel, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "8% of linehaul"),
            Line(SeedCodes.TrkToll, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m, "Pass-through"),
            Line(SeedCodes.TrkDrop, OperationalServiceType.Standard, 1, MarkupType.Percentage, 10m, "1 additional drop"));

        cancelledLaneQuotation.Approve("Seed Data");
        await Save(cancelledLaneQuotation);

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedQuotations = created;
        LogCompleted(created.Count);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private readonly record struct LineSpec(
       string ChargeCode,
       OperationalServiceType ServiceType,
       decimal Quantity,
       MarkupType MarkupType,
       decimal MarkupValue,
       string? Notes = null,
       decimal? BaseAmount = null);

    private static LineSpec Line(
        string chargeCode,
        OperationalServiceType serviceType,
        decimal quantity,
        MarkupType markupType,
        decimal markupValue,
        string? notes = null,
        decimal? baseAmount = null) =>
        new(chargeCode, serviceType, quantity, markupType, markupValue, notes, baseAmount);

    /// <summary>
    /// Adds lines by charge code. Category comes from the rate's ChargeCode (which TariffSeeder attaches),
    /// and fxRate is null because each tariff is already in its quotation's currency.
    /// </summary>
    private static void AddLines(Quotation quotation, IReadOnlyList<TariffRate> tariffRates, params LineSpec[] specs)
    {
        foreach (var spec in specs)
        {
            var rate = tariffRates.Rate(spec.ChargeCode);

            quotation.AddLine(
                tariffRate: rate,
                category: rate.ChargeCode!.Category,
                serviceType: spec.ServiceType,
                quantity: spec.Quantity,
                markupType: spec.MarkupType,
                markupValue: spec.MarkupValue,
                baseAmount: spec.BaseAmount ?? rate.BaseAmount, // Snapshots rate.BaseAmount or applies override
                fxRate: null,
                notes: spec.Notes);
        }
    }

    /// <summary>The line set a chilled GDP load needs to pass Submit()'s operational integrity checks.</summary>
    private static void AddReeferLines(Quotation quotation, IReadOnlyList<TariffRate> reeferRates, decimal markup) =>
        AddLines(quotation, reeferRates,
            Line(SeedCodes.ReefLinehaul, OperationalServiceType.LineHaul, 1, MarkupType.Percentage, markup, "Cold Chain Linehaul"),
            Line(SeedCodes.ReefPti, OperationalServiceType.PreCoolingAndPTI, 1, MarkupType.Percentage, markup, "Pre-Cooling and PTI Inspection"),
            Line(SeedCodes.ReefTelematics, OperationalServiceType.ContinuousTelematics, 1, MarkupType.Percentage, markup, "GDP Telemetry Fee"),
            Line(SeedCodes.ReefGenset, OperationalServiceType.GensetFuel, 10, MarkupType.Percentage, 10m, "~10 running hours"),
            Line(SeedCodes.ReefPlugin, OperationalServiceType.ColdStoragePlugin, 2, MarkupType.Flat, 0m, "2 days plug-in at DC (1 free)"),
            Line(SeedCodes.TrkToll, OperationalServiceType.Standard, 1, MarkupType.Flat, 0m));
}
