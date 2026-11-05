using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the branch list - the scoping root every other Master Data / Freight Forwarding seeder
/// hangs off. Fixed set matching the anchor tenant's real offices (see feature-map.md's "On
/// Branch, honestly"); not offered for a solo owner-operator tenant, which has no branch network.
/// </summary>
internal class BranchSeeder(ILogger<BranchSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(BranchSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 200;

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Branch>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        if (context.OperatingMode is OperatingMode.SoloOperator)
        {
            LogSkipping("solo owner-operator tenants have no branch network");
            context.CreatedBranches = [];
            LogCompleted(0);
            return;
        }

        var repo = context.TenantUnitOfWork.Repository<Branch>();
        var seeds = new (string Code, string Name, string ShortName)[]
        {
            ("MNL", "Manila Head Office", "MNL"),
            ("CEB", "Cebu Branch", "CEB"),
            ("DVO", "Davao Branch", "DVO"),
            ("CGY", "Cagayan de Oro Branch", "CDO"),
            ("CRK", "Clark Branch", "CRK")
        };

        var created = new List<Branch>();
        foreach (var seed in seeds)
        {
            var branch = new Branch
            {
                Code = seed.Code,
                Name = seed.Name,
                ShortName = seed.ShortName
            };
            await repo.AddAsync(branch, cancellationToken);
            created.Add(branch);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedBranches = created;
        LogCompleted(created.Count);
    }
}
