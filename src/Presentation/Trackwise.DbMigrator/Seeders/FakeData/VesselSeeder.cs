using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds vessels - the first place a ship has an identity, per Vessel's own doc comment. Mixed
/// types, and deliberately only some carry an IMO number: most of the domestic fleet genuinely
/// has none.
/// </summary>
internal class VesselSeeder(ILogger<VesselSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(VesselSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 205;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Vessel>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var branches = context.CreatedBranches;
        if (branches is null)
        {
            LogUpstreamSkipped();
            return;
        }

        if (branches.Count == 0)
        {
            context.CreatedVessels = [];
            LogCompleted(0);
            return;
        }

        var manila = branches.FirstOrDefault(b => b.Code == "MNL");
        var repo = context.TenantUnitOfWork.Repository<Vessel>();
        var seeds = new (string Code, string Name, string? Imo, VesselType Type, int? Teu, string? Flag, Guid? BranchId)[]
        {
            ("VSL-001", "MV Skybest Pioneer", "9395044", VesselType.ContainerShip, 1200, "PH", manila?.Id),
            ("VSL-002", "MV Visayas Trader", null, VesselType.RoRo, null, "PH", null),
            ("VSL-003", "Amihan Barge 2", null, VesselType.Barge, null, "PH", null),
            ("VSL-004", "MV Cebu Feeder", "9512345", VesselType.Feeder, 450, "PA", null)
        };

        var created = new List<Vessel>();
        foreach (var seed in seeds)
        {
            var vessel = new Vessel
            {
                Code = seed.Code,
                Name = seed.Name,
                ImoNumber = seed.Imo,
                Type = seed.Type,
                TeuCapacity = seed.Teu,
                FlagCountryCode = seed.Flag,
                BranchId = seed.BranchId
            };
            await repo.AddAsync(vessel, cancellationToken);
            created.Add(vessel);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedVessels = created;
        LogCompleted(created.Count);
    }
}
