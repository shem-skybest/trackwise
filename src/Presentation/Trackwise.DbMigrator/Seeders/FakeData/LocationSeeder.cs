using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds locations - delivery points, a container yard, and one self-referencing zone, so the
/// module's first parent/child master entity has real data to exercise the dependency probe and
/// merge repoint against.
/// </summary>
internal class LocationSeeder(ILogger<LocationSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(LocationSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 207;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Location>().CountAsync(ct: cancellationToken) > 0;
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
            context.CreatedLocations = [];
            LogCompleted(0);
            return;
        }

        var repo = context.TenantUnitOfWork.Repository<Location>();
        var created = new List<Location>();

        var zone = new Location { Code = "LOC-001", Name = "Metro Manila Zone", Type = LocationType.Zone };
        await repo.AddAsync(zone, cancellationToken);
        created.Add(zone);

        var yard = new Location { Code = "LOC-002", Name = "Manila North Harbor Container Yard", Type = LocationType.ContainerYard };
        await repo.AddAsync(yard, cancellationToken);
        created.Add(yard);

        // Saved so the zone has an id to parent against before the points below reference it.
        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);

        var points = new (string Code, string Name)[]
        {
            ("LOC-003", "Makati CBD Warehouse"),
            ("LOC-004", "Quezon City Distribution Center")
        };

        foreach (var (code, name) in points)
        {
            var point = new Location
            {
                Code = code,
                Name = name,
                Type = LocationType.DeliveryPoint,
                ParentLocationId = zone.Id
            };
            await repo.AddAsync(point, cancellationToken);
            created.Add(point);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedLocations = created;
        LogCompleted(created.Count);
    }
}
