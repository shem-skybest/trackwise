using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.DbMigrator.Regions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Application.Abstractions.Tenancy;
using Npgsql;
using System.Text.RegularExpressions;

namespace Trackwise.DbMigrator.Seeders.Infrastructure;

/// <summary>
/// Reads <c>Tenants[]</c> from configuration and upserts each demo tenant in the master DB,
/// provisioning its tenant database if missing. Replaces the legacy single-tenant
/// <c>DefaultTenantSeeder</c>.
/// </summary>
internal sealed partial class DemoTenantsSeeder(
    ILogger<DemoTenantsSeeder> logger,
    IRegionProfileFactory regionProfileFactory) : SeederBase(logger)
{
    public override string Name => nameof(DemoTenantsSeeder);
    public override SeederType Type => SeederType.Infrastructure;
    public override int Order => 40;

    /// <summary>
    /// Demo threshold for the broker credit booking gate - the Demo load-board provider
    /// generates scores 30-100, so ~half its brokers trip the gate for demo purposes.
    /// </summary>
    private const int DemoMinBrokerCreditScore = 70;

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var tenantConfigs = DemoTenantConfig.Load(context.Configuration, context.IsProduction);
        if (tenantConfigs.Count == 0)
        {
            logger.LogWarning("No 'Tenants' configured. Skipping demo tenant seeding.");
            LogCompleted();
            return;
        }

        var repo = context.MasterUnitOfWork.Repository<Tenant>();
        var databaseProvider = context.ServiceProvider.GetRequiredService<ITenantDatabaseService>();

        // Backfill: rename legacy "default" → "us" if found, so devs with existing master DBs
        // don't end up with both. The "us" tenant config supersedes "default".
        if (tenantConfigs.Any(c => c.Name == "us"))
        {
            var legacy = await repo.GetAsync(t => t.Name == "default", cancellationToken);
            if (legacy is not null && await repo.GetAsync(t => t.Name == "us", cancellationToken) is null)
            {
                legacy.Name = "us";
                await context.MasterUnitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Renamed legacy tenant 'default' → 'us' in master DB");
            }
        }

        // Backfill: rename legacy "us" → "ph" (and its physical database), so devs with an
        // existing master DB from before the PH rename don't end up with both a stale "us"
        // tenant and a freshly re-seeded, disconnected "ph" one.
        if (tenantConfigs.Any(c => c.Name == "ph"))
        {
            var legacy = await repo.GetAsync(t => t.Name == "us", cancellationToken);
            if (legacy is not null && await repo.GetAsync(t => t.Name == "ph", cancellationToken) is null)
            {
                var renamedConnectionString = await RenameTenantDatabaseAsync(legacy.ConnectionString, cancellationToken);
                legacy.Name = "ph";
                legacy.ConnectionString = renamedConnectionString;
                await context.MasterUnitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Renamed legacy tenant 'us' → 'ph' in master DB");
            }
        }

        foreach (var config in tenantConfigs)
        {
            var profile = regionProfileFactory.Get(config.Region);
            var connectionString = config.ResolveConnectionString(context.Configuration)
                ?? throw new InvalidOperationException(
                    $"No connection string for tenant '{config.Name}'. Set Tenants[].ConnectionString " +
                    $"or ConnectionStrings:{config.ConnectionStringKey()}.");
            var existing = await repo.GetAsync(t => t.Name == config.Name, cancellationToken);

            if (existing is null)
            {
                var tenant = new Tenant
                {
                    Name = config.Name,
                    CompanyName = config.CompanyName,
                    SystemName = config.SystemName,
                    BillingEmail = config.BillingEmail,
                    CompanyAddress = profile.CompanyAddress,
                    ConnectionString = connectionString,
                    IsSubscriptionRequired = false
                };
                ApplyRegionalSettings(tenant, profile);
                ApplyDemoTaxIdentifiers(tenant, profile);
                tenant.Settings.MinBrokerCreditScore = DemoMinBrokerCreditScore;
                tenant.Settings.DefaultRateFloorPerMile = profile.DefaultRateFloorPerMile;
                tenant.Settings.OperatingMode = config.OperatingMode;

                await repo.AddAsync(tenant, cancellationToken);
                await context.MasterUnitOfWork.SaveChangesAsync(cancellationToken);
                await databaseProvider.CreateDatabaseAsync(tenant.ConnectionString);
                logger.LogInformation("Created tenant '{Name}' ({Region})", tenant.Name, profile.Region);
            }
            else
            {
                var updated = false;
                if (existing.CompanyName != config.CompanyName) { existing.CompanyName = config.CompanyName; updated = true; }
                if (existing.SystemName is null && config.SystemName is not null) { existing.SystemName = config.SystemName; updated = true; }
                if (existing.BillingEmail != config.BillingEmail) { existing.BillingEmail = config.BillingEmail; updated = true; }
                if (existing.ConnectionString != connectionString) { existing.ConnectionString = connectionString; updated = true; }
                if (existing.Settings.Region != profile.Region)
                {
                    ApplyRegionalSettings(existing, profile);
                    updated = true;
                }
                if (ApplyDemoTaxIdentifiers(existing, profile))
                {
                    updated = true;
                }
                if (existing.Settings.MinBrokerCreditScore is null)
                {
                    existing.Settings.MinBrokerCreditScore = DemoMinBrokerCreditScore;
                    updated = true;
                }
                if (existing.Settings.DefaultRateFloorPerMile is null)
                {
                    existing.Settings.DefaultRateFloorPerMile = profile.DefaultRateFloorPerMile;
                    updated = true;
                }
                if (existing.Settings.OperatingMode != config.OperatingMode)
                {
                    existing.Settings.OperatingMode = config.OperatingMode;
                    updated = true;
                }

                if (updated)
                {
                    await context.MasterUnitOfWork.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Updated tenant '{Name}'", existing.Name);
                }

                // Ensure DB exists even for existing tenants (idempotent - no-op if present).
                await databaseProvider.CreateDatabaseAsync(existing.ConnectionString);
            }
        }

        LogCompleted(tenantConfigs.Count);
    }

    /// <summary>
    /// Renames the tenant's physical database in place (e.g. <c>us_trackwise</c> -&gt;
    /// <c>ph_trackwise</c>), preserving its data, and returns the connection string pointed at
    /// the new name. Active connections are terminated first - Postgres refuses to rename a
    /// database anyone is connected to.
    /// </summary>
    private async Task<string> RenameTenantDatabaseAsync(
        string oldConnectionString, CancellationToken cancellationToken)
    {
        var oldBuilder = new NpgsqlConnectionStringBuilder(oldConnectionString);
        var oldDbName = oldBuilder.Database
            ?? throw new InvalidOperationException("Legacy tenant connection string has no database name.");
        var newDbName = TenantDatabaseNameRegex().Replace(oldDbName, "ph");

        var maintenanceBuilder = new NpgsqlConnectionStringBuilder(oldConnectionString) { Database = "postgres" };
        await using var connection = new NpgsqlConnection(maintenanceBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var terminateCmd = new NpgsqlCommand(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{oldDbName}' AND pid <> pg_backend_pid()",
            connection))
        {
            await terminateCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var renameCmd = new NpgsqlCommand(
            $"ALTER DATABASE \"{oldDbName}\" RENAME TO \"{newDbName}\"", connection))
        {
            await renameCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("Renamed database '{OldName}' -> '{NewName}'", oldDbName, newDbName);

        var newBuilder = new NpgsqlConnectionStringBuilder(oldConnectionString) { Database = newDbName };
        return newBuilder.ConnectionString;
    }

    [GeneratedRegex("^[a-z]+")]
    private static partial Regex TenantDatabaseNameRegex();

    private static void ApplyRegionalSettings(Tenant tenant, IRegionProfile profile) =>
        tenant.Settings.ApplyRegionDefaults(profile.Region);

    private static bool ApplyDemoTaxIdentifiers(Tenant tenant, IRegionProfile profile)
    {
        var updated = false;

        if (string.IsNullOrWhiteSpace(tenant.TaxResidencyCountry))
        {
            tenant.TaxResidencyCountry = profile.CountryCode;
            updated = true;
        }

        if (profile.Region != Trackwise.Domain.Primitives.Enums.Region.EU)
        {
            return updated;
        }

        if (string.IsNullOrWhiteSpace(tenant.VatNumber))
        {
            tenant.VatNumber = "DE123456789";
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(tenant.EoriNumber))
        {
            tenant.EoriNumber = "DE123456789000";
            updated = true;
        }

        if (string.IsNullOrWhiteSpace(tenant.CompanyRegistrationNumber))
        {
            tenant.CompanyRegistrationNumber = "HRB 123456";
            updated = true;
        }

        return updated;
    }
}
