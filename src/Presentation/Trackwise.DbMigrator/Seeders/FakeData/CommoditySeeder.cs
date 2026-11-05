using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds commodities - the first canonical list of what is being moved, replacing the free-text
/// sprawl <c>Workfile.Commodity</c> copied from nSFS. Includes one hazardous and one
/// temperature-controlled line so both flags have real data to exercise.
/// </summary>
internal class CommoditySeeder(ILogger<CommoditySeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(CommoditySeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 206;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Commodity>().CountAsync(ct: cancellationToken) > 0;
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
            context.CreatedCommodities = [];
            LogCompleted(0);
            return;
        }

        var repo = context.TenantUnitOfWork.Repository<Commodity>();
        var seeds = new (string Code, string Name, string? Hs, bool Hazmat, string? Un, bool Reefer)[]
        {
            ("COM-001", "General Merchandise", "999999", false, null, false),
            ("COM-002", "Consumer Electronics", "851762", false, null, false),
            ("COM-003", "Frozen Fish", "030389", false, null, true),
            ("COM-004", "Machinery Parts", "847989", false, null, false),
            ("COM-005", "Flammable Liquids, N.O.S.", "271019", true, "1993", false),
            ("COM-006", "Rice and Grains", "100630", false, null, false)
        };

        var created = new List<Commodity>();
        foreach (var seed in seeds)
        {
            var commodity = new Commodity
            {
                Code = seed.Code,
                Name = seed.Name,
                HsCode = seed.Hs,
                IsHazardous = seed.Hazmat,
                UnNumber = seed.Un,
                RequiresTemperatureControl = seed.Reefer
            };
            await repo.AddAsync(commodity, cancellationToken);
            created.Add(commodity);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedCommodities = created;
        LogCompleted(created.Count);
    }
}
