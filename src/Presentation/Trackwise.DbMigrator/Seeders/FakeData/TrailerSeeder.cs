using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds trailers - the boring half of Fleet: a new table with no dependents yet, taking the
/// vessel/location shape unchanged. Deliberately not linked to any Truck (see Trailer's own doc
/// comment on why). A tenant with a real vehicle register (<c>Trailers</c> seed section) gets its
/// real trailers, per record and keyed on the code; any other tenant gets four placeholders.
/// </summary>
internal class TrailerSeeder(ILogger<TrailerSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(TrailerSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 208;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        if (context.GetSeedTrailers().Length > 0)
        {
            return false;
        }

        return await context.TenantUnitOfWork.Repository<Trailer>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        // Real trailers are seeded per record, so they must also land on a database whose branches
        // were seeded on an earlier run - read the branches back rather than wait for this run's.
        var seedTrailers = context.GetSeedTrailers();
        if (seedTrailers.Length > 0)
        {
            var branchList = await context.TenantUnitOfWork.Repository<Branch>().GetListAsync(ct: cancellationToken);
            var seeded = await SeedRegisterAsync(context, seedTrailers, branchList, cancellationToken);
            await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
            context.CreatedTrailers = seeded;
            LogCompleted(seeded.Count);
            return;
        }

        var branches = context.CreatedBranches;
        if (branches is null)
        {
            LogUpstreamSkipped();
            return;
        }

        if (branches.Count == 0 || context.IsProduction)
        {
            context.CreatedTrailers = [];
            LogCompleted(0);
            return;
        }

        var created = await SeedPlaceholdersAsync(context, cancellationToken);
        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedTrailers = created;
        LogCompleted(created.Count);
    }

    private async Task<List<Trailer>> SeedRegisterAsync(
        SeederContext context,
        FleetTrailerData[] seeds,
        IEnumerable<Branch> branches,
        CancellationToken ct)
    {
        var repo = context.TenantUnitOfWork.Repository<Trailer>();
        var existingCodes = (await repo.GetListAsync(ct: ct))
            .Select(t => t.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var branchesByCode = branches.ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);
        var created = new List<Trailer>();

        foreach (var seed in seeds)
        {
            var code = Trailer.NormalizeCode(seed.Code);
            if (!existingCodes.Add(code))
            {
                continue;
            }

            if (!Enum.TryParse<TrailerType>(seed.Type, out var type))
            {
                logger.LogWarning("Unknown trailer type '{Type}' for {Code} - skipped", seed.Type, code);
                continue;
            }

            var trailer = new Trailer
            {
                Code = code,
                Name = seed.Plate is { } plate ? $"{plate} ({type.GetDescription()})" : $"{code} ({type.GetDescription()})",
                Type = type,
                LicensePlate = seed.Plate,
                LicensePlateState = "PH",
                Notes = seed.Notes,
                IsActive = seed.IsActive,
                BranchId = seed.Branch is { } branchCode && branchesByCode.TryGetValue(branchCode, out var branch)
                    ? branch.Id
                    : null
            };
            await repo.AddAsync(trailer, ct);
            created.Add(trailer);
        }

        return created;
    }

    private static async Task<List<Trailer>> SeedPlaceholdersAsync(SeederContext context, CancellationToken ct)
    {
        var repo = context.TenantUnitOfWork.Repository<Trailer>();
        var seeds = new (string Code, string Name, TrailerType Type, string? Vin, string Plate)[]
        {
            ("TRL-001", "Dry Van Trailer 01", TrailerType.DryVan, "1GRAA0629PJ123456", "UAB-1234"),
            ("TRL-002", "Reefer Trailer 01", TrailerType.Reefer, "1GRAA0629PJ654321", "UAB-5678"),
            ("TRL-003", "Flatbed Trailer 01", TrailerType.Flatbed, null, "UAB-9012"),
            ("TRL-004", "40' Container Chassis 01", TrailerType.ContainerChassis, null, "UAB-3456")
        };

        var created = new List<Trailer>();
        foreach (var seed in seeds)
        {
            var trailer = new Trailer
            {
                Code = seed.Code,
                Name = seed.Name,
                Type = seed.Type,
                Vin = seed.Vin,
                LicensePlate = seed.Plate,
                LicensePlateState = "PH"
            };
            await repo.AddAsync(trailer, ct);
            created.Add(trailer);
        }

        return created;
    }
}
