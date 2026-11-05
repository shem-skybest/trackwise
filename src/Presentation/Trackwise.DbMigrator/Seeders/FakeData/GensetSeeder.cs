using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds gensets - a small, deliberately short list. Genset is a scarce shared physical resource
/// (see the Cold Chain handoff doc, §3), not a fleet-sized fake-data table: a real CCL operation
/// runs on a handful of units.
/// </summary>
internal class GensetSeeder(ILogger<GensetSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(GensetSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 209;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Genset>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<Genset>();
        var seeds = new (string Code, string Name, GensetStatus Status, string SerialNumber, string Make, string Model)[]
        {
            ("GEN-001", "G1", GensetStatus.Available, "SN-00142", "Cummins", "C88D5"),
            ("GEN-002", "G2", GensetStatus.InUse, "SN-00143", "Cummins", "C88D5"),
        };

        var created = new List<Genset>();
        foreach (var seed in seeds)
        {
            var genset = new Genset
            {
                Code = seed.Code,
                Name = seed.Name,
                Status = seed.Status,
                SerialNumber = seed.SerialNumber,
                Make = seed.Make,
                Model = seed.Model
            };
            await repo.AddAsync(genset, cancellationToken);
            created.Add(genset);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        LogCompleted(created.Count);
    }
}
