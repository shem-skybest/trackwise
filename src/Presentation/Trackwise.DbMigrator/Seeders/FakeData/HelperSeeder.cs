using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds helpers - standalone personnel records, deliberately not built on Employee (see Helper's
/// own doc comment on why).
/// </summary>
internal class HelperSeeder(ILogger<HelperSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(HelperSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 209;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Helper>().CountAsync(ct: cancellationToken) > 0;
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

        // A tenant with a real crew roster (CrewSeeder) gets its real helpers, not these placeholders.
        if (branches.Count == 0 || context.GetSeedCrew().Length > 0)
        {
            context.CreatedHelpers = [];
            LogCompleted(0);
            return;
        }

        var repo = context.TenantUnitOfWork.Repository<Helper>();
        var seeds = new (string Code, string Name, string Phone)[]
        {
            ("HLP-001", "Ramon Cruz", "+63 917 100 0001"),
            ("HLP-002", "Ariel Santos", "+63 917 100 0002"),
            ("HLP-003", "Bong Villanueva", "+63 917 100 0003"),
            ("HLP-004", "Dennis Reyes", "+63 917 100 0004")
        };

        var created = new List<Helper>();
        foreach (var seed in seeds)
        {
            var helper = new Helper
            {
                Code = seed.Code,
                Name = seed.Name,
                PhoneNumber = seed.Phone
            };
            await repo.AddAsync(helper, cancellationToken);
            created.Add(helper);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedHelpers = created;
        LogCompleted(created.Count);
    }
}
