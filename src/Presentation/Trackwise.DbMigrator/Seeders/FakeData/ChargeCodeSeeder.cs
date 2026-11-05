using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds charge codes - the vocabulary tariffs price against. Covers every <see cref="ChargeCategory"/>
/// so a demo tariff (<see cref="TariffSeeder"/>) has a realistic mix to attach rates to.
/// </summary>
internal class ChargeCodeSeeder(ILogger<ChargeCodeSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(ChargeCodeSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 210;

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<ChargeCode>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        if (context.OperatingMode is OperatingMode.SoloOperator)
        {
            LogSkipping("solo owner-operator tenants have no freight-forwarding charges");
            context.CreatedChargeCodes = [];
            LogCompleted(0);
            return;
        }

        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<ChargeCode>();
        var allModes = new[] { FreightMode.Domestic, FreightMode.Import, FreightMode.Export, FreightMode.Triangular };
        var oceanModes = new[] { FreightMode.Import, FreightMode.Export, FreightMode.Triangular };

        var seeds = new (string Code, string Name, ChargeCategory Category, FreightMode[] Modes, string Unit, bool RequiresApproval)[]
        {
            ("OFR", "Ocean Freight", ChargeCategory.Base, oceanModes, "per container", false),
            ("TRK", "Trucking Freight", ChargeCategory.Base, [FreightMode.Domestic], "per trip", false),
            ("BAF", "Bunker Adjustment Factor", ChargeCategory.Surcharge, oceanModes, "percentage", false),
            ("THC", "Terminal Handling Charge", ChargeCategory.Accessorial, oceanModes, "per container", false),
            ("DEM", "Demurrage", ChargeCategory.Accessorial, oceanModes, "per day", true),
            ("DOC", "Documentation Fee", ChargeCategory.Accessorial, allModes, "per shipment", false),
            ("INS", "Cargo Insurance", ChargeCategory.Accessorial, allModes, "percentage", true),
            ("VAT", "Value Added Tax", ChargeCategory.Tax, allModes, "percentage", false)
        };

        var created = new List<ChargeCode>();
        foreach (var seed in seeds)
        {
            var chargeCode = ChargeCode.Create(
                seed.Code, seed.Name, seed.Category, seed.Modes, seed.Unit, seed.RequiresApproval);
            await repo.AddAsync(chargeCode, cancellationToken);
            created.Add(chargeCode);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedChargeCodes = created;
        LogCompleted(created.Count);
    }
}
