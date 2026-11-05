using System.Globalization;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds each client's real delivery outlets and pickup warehouses (the <c>Outlets</c> seed
/// section) as locations owned by that customer. Like <see cref="FleetSeeder" /> it seeds per
/// record, keyed on the code, so it adds to a database that already has locations. An outlet already
/// seeded only has its town corrected: filled in when it was left blank, or moved to the seed's
/// province when the town is the same (Antipolo was first seeded under Metro Manila); anything else
/// is left as staff edited it.
/// </summary>
internal class OutletSeeder(ILogger<OutletSeeder> log) : SeederBase(log)
{
    public override string Name => nameof(OutletSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 212;
    public override IReadOnlyList<string> DependsOn => [nameof(CustomerSeeder), nameof(LocationSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedOutlets().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<Location>();
        var existing = (await repo.GetListAsync(ct: cancellationToken))
            .GroupBy(l => l.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var customersByName = (await context.TenantUnitOfWork.Repository<Customer>().GetListAsync(ct: cancellationToken))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var missingCustomers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var corrected = 0;

        foreach (var outlet in context.GetSeedOutlets())
        {
            var code = Location.NormalizeCode(outlet.Code);
            if (existing.TryGetValue(code, out var seeded))
            {
                if (CorrectTown(seeded, outlet))
                {
                    corrected++;
                }

                continue;
            }

            if (!customersByName.TryGetValue(outlet.Customer, out var customerId))
            {
                if (missingCustomers.Add(outlet.Customer))
                {
                    logger.LogWarning("Customer '{Customer}' not found - its outlets were skipped", outlet.Customer);
                }

                continue;
            }

            await repo.AddAsync(new Location
            {
                Code = code,
                Name = outlet.Name,
                Type = outlet.Type,
                CustomerId = customerId,
                Address = outlet.Address,
                Latitude = outlet.Latitude,
                Longitude = outlet.Longitude,
                DefaultCutoffTime = TimeOnly.TryParseExact(
                    outlet.DefaultCutoff, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var cutoff)
                    ? cutoff
                    : null,
                Notes = BuildNotes(outlet)
            }, cancellationToken);

            created++;
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        if (corrected > 0)
        {
            logger.LogInformation("Corrected the town of {Count} outlets already seeded", corrected);
        }

        LogCompleted(created);
    }

    private static bool CorrectTown(Location location, OutletData outlet)
    {
        if (outlet.Address is not { } seed || string.IsNullOrWhiteSpace(seed.CityOrMunicipality) || location.Address is not { } stored)
        {
            return false;
        }

        var blank = string.IsNullOrWhiteSpace(stored.CityOrMunicipality);
        var sameTownOtherProvince =
            string.Equals(stored.CityOrMunicipality, seed.CityOrMunicipality, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(stored.Province, seed.Province, StringComparison.OrdinalIgnoreCase);

        if (!blank && !sameTownOtherProvince)
        {
            return false;
        }

        location.Address = stored with
        {
            CityOrMunicipality = seed.CityOrMunicipality,
            Province = seed.Province,
            Region = seed.Region
        };
        return true;
    }

    /// <summary>
    /// Provenance a steward needs to judge the record: how much it was used, what else it was
    /// called, and whether the address is only as good as a city-centre pin.
    /// </summary>
    private static string BuildNotes(OutletData outlet)
    {
        var lines = new List<string>
        {
            outlet.Source
            ?? (outlet.Stops == 1
                ? $"Monday delivery board: 1 stop, {outlet.FirstDelivery}."
                : $"Monday delivery board: {outlet.Stops} stops, {outlet.FirstDelivery} to {outlet.LastDelivery}.")
        };

        if (outlet.Aliases.Length > 0)
        {
            lines.Add($"Also written as: {string.Join(", ", outlet.Aliases)}.");
        }

        if (outlet.Address is null)
        {
            lines.Add("No usable map pin on Monday - add the address.");
        }
        else if (outlet.CityLevelPin)
        {
            lines.Add("Map pin is only the city centre - confirm the street address and pin.");
        }

        return string.Join("\n", lines);
    }
}
