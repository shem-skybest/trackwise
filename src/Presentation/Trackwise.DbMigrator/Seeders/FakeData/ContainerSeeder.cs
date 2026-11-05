using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Extensions;
using Trackwise.DbMigrator.Models;
using Trackwise.DbMigrator.Utils;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds intermodal containers - mixed ISO types and statuses, ~70% sitting at a terminal.
/// </summary>
internal class ContainerSeeder(ILogger<ContainerSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(ContainerSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 135;
    public override IReadOnlyList<string> DependsOn => [nameof(TerminalSeeder)];

    /// <summary>
    /// The mix, as master-data codes rather than enum members. Repeats are the weighting: a yard
    /// is mostly 40' boxes. Resolved against the rows the AddContainerTypesAndCurrencies migration
    /// seeds, so this list and the table cannot drift without the seeder saying so.
    /// </summary>
    private static readonly string[] CommonTypeCodes =
        ["GP20", "GP40", "GP40", "HC40", "HC40", "HC45", "RF40", "TK20"];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Container>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var region = context.Region ?? throw new InvalidOperationException("Region profile not set");
        var terminals = context.CreatedTerminals;
        if (terminals is null)
        {
            LogUpstreamSkipped();
            return;
        }

        if (terminals.Count == 0)
        {
            logger.LogWarning("No terminals available - skipping containers");
            context.CreatedContainers = [];
            LogCompleted(0);
            return;
        }

        var containerTypes = await context.TenantUnitOfWork.Repository<ContainerType>()
            .Query()
            .Where(t => CommonTypeCodes.Contains(t.Code))
            .ToDictionaryAsync(t => t.Code, cancellationToken);

        var typePool = CommonTypeCodes
            .Where(containerTypes.ContainsKey)
            .Select(code => containerTypes[code])
            .ToArray();

        if (typePool.Length == 0)
        {
            logger.LogWarning("No container types available - skipping containers");
            context.CreatedContainers = [];
            LogCompleted(0);
            return;
        }

        var repo = context.TenantUnitOfWork.Repository<Container>();
        var ownerCodes = region.ContainerOwnerCodes;
        var created = new List<Container>();

        var containerCount = context.Scale(30);

        for (var i = 0; i < containerCount; i++)
        {
            var ownerCode = random.Pick((IList<string>)ownerCodes);
            var containerType = random.Pick((IList<ContainerType>)typePool);
            var roll = random.NextDouble();

            var container = new Container
            {
                Number = IsoContainerNumber.Generate(ownerCode, random),
                ContainerTypeId = containerType.Id,
                IsLaden = false
            };
            await repo.AddAsync(container, cancellationToken);

            // Initial state mix:
            //  ~30% sitting empty in a depot, ~40% laden at a port, ~30% empty (no terminal yet).
            if (roll < 0.30)
            {
                container.MarkAtPort(random.Pick(terminals));
            }
            else if (roll < 0.70)
            {
                container.MarkAtPort(random.Pick(terminals));
                container.MarkAsLoaded();
            }
            // else: leave as default Empty status with no terminal

            created.Add(container);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedContainers = created;
        LogCompleted(created.Count);
    }
}
