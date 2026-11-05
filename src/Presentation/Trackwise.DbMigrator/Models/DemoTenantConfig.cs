using Trackwise.Domain.Primitives.Enums;
using Microsoft.Extensions.Configuration;

namespace Trackwise.DbMigrator.Models;

/// <summary>
/// Configuration for a demo tenant to be seeded by the DbMigrator.
/// Loaded from the "Tenants" section of appsettings.json.
/// </summary>
public sealed record DemoTenantConfig
{
    public required string Name { get; init; }
    public required string CompanyName { get; init; }

    /// <summary>Seeds <c>Tenant.SystemName</c> only while it is empty, so an edit in Settings sticks.</summary>
    public string? SystemName { get; init; }
    public required string BillingEmail { get; init; }
    public required Region Region { get; init; }

    /// <summary>
    /// <c>SeedData/*.json</c> section this tenant draws its users and logins from; defaults to the
    /// region name. Two tenants in the same region need distinct keys or one re-homes the other's logins.
    /// </summary>
    public string? SeedDataKey { get; init; }

    public OperatingMode OperatingMode { get; init; } = OperatingMode.Fleet;

    /// <summary>Multiplier on the fake-data volumes; 1.0 is the fleet-sized demo.</summary>
    public double DataScale { get; init; } = 1.0;

    /// <summary>
    /// Optional explicit connection string. When omitted, DemoTenantsSeeder reads the
    /// <c>ConnectionStrings:*TenantDatabase</c> slot for this tenant name.
    /// </summary>
    public string? ConnectionString { get; init; }

    /// <summary>A purely fictional tenant (e.g. the US <c>solo</c> owner-operator) that is never provisioned in Production.</summary>
    public bool DemoOnly { get; init; }

    /// <summary>The configured <c>Tenants</c>, minus the <see cref="DemoOnly"/> ones when seeding Production.</summary>
    public static List<DemoTenantConfig> Load(IConfiguration configuration, bool isProduction)
    {
        var configs = configuration.GetSection("Tenants").Get<List<DemoTenantConfig>>() ?? [];
        return isProduction ? configs.Where(c => !c.DemoOnly).ToList() : configs;
    }

    public string ResolveSeedDataKey() =>
        string.IsNullOrWhiteSpace(SeedDataKey) ? Region.ToString() : SeedDataKey;

    /// <summary>
    /// Resolves the connection string the migrator itself uses to reach this tenant's database -
    /// e.g. the Docker-internal "postgres" host in local dev. Never the value to persist for
    /// other services to read; see <c>SeederOrchestrationWorker</c>'s Pass 3 for that.
    /// </summary>
    public string? ResolveConnectionString(IConfiguration configuration) =>
        string.IsNullOrWhiteSpace(ConnectionString)
            ? configuration.GetConnectionString(ConnectionStringKey())
            : ConnectionString;

    /// <summary>
    /// The <c>ConnectionStrings:*TenantDatabase</c> slot for this tenant, set by <c>deploy/.env</c>
    /// in a container deployment.
    /// </summary>
    public string ConnectionStringKey()
    {
        // A region code is an acronym ("ph" -> PHTenantDatabase); any other name is a word.
        if (Enum.TryParse<Region>(Name, ignoreCase: true, out var region))
        {
            return $"{region}TenantDatabase";
        }

        return $"{char.ToUpperInvariant(Name[0])}{Name[1..]}TenantDatabase";
    }
}
