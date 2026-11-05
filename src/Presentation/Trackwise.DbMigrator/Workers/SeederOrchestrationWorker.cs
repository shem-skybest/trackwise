using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.DbMigrator.Regions;
using Trackwise.DbMigrator.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Options;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace Trackwise.DbMigrator.Workers;

/// <summary>
/// Orchestrates seeders across one or more demo tenants.
///
/// Pass 1 - global Infrastructure seeders run once against master DB
///          (roles, super admin, plans, Stripe, DemoTenantsSeeder which provisions tenants).
/// Pass 2 - for each demo tenant, mutate the singleton TenantDbContextOptions.ConnectionString,
///          open a fresh DI scope (which produces a TenantDbContext bound to that tenant's DB),
///          and run the tenant-scoped seeders (Infrastructure-tenant-scoped + all FakeData).
/// Pass 3 - now that nothing in this run needs to reach a tenant DB anymore, rewrite each
///          tenant's persisted ConnectionString to the host-facing form: same database name as
///          whatever Passes 1-2 actually migrated (Tenants[].ConnectionString, e.g. a custom
///          name from docker-compose), with the host/port/credentials swapped to
///          TenantDatabaseDefaults. Passes 1-2 deliberately keep using Tenants[].ConnectionString
///          throughout - typically the Docker-internal host in local dev - because that's what
///          the migrator itself needs to reach Postgres; only host-run consumers (API,
///          IdentityServer) should see the host-facing value.
/// </summary>
internal class SeederOrchestrationWorker(
    ILogger<SeederOrchestrationWorker> logger,
    IServiceScopeFactory scopeFactory,
    TenantDatabaseDefaults tenantDatabaseDefaults,
    IHostEnvironment environment)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var rootScope = scopeFactory.CreateScope();
        var configuration = rootScope.ServiceProvider.GetRequiredService<IConfiguration>();

        var allSeeders = rootScope.ServiceProvider.GetServices<ISeeder>();
        var orderedSeeders = SeederRegistry.GetOrderedSeeders(allSeeders, logger);

        // Filtered after ordering, so a production seeder may still name a demo-only one in DependsOn.
        if (environment.IsProduction())
        {
            orderedSeeders = orderedSeeders.Where(s => s.SeedsInProduction).ToList();
            logger.LogInformation("Production: running only {Seeders}",
                string.Join(", ", orderedSeeders.Select(s => s.Name)));
        }

        var tenantConfigs = DemoTenantConfig.Load(configuration, environment.IsProduction());
        try
        {
            await SeedAsync(rootScope, configuration, orderedSeeders, tenantConfigs, cancellationToken);
        }
        finally
        {
            // A failed seeder must not leave the Docker-internal host persisted: the host-run API
            // and IdentityServer would hang resolving it on every tenant request.
            await FinalizeConnectionStringsAsync(rootScope, configuration, tenantConfigs, cancellationToken);
        }

        logger.LogInformation("Database seeding completed successfully");
    }

    private async Task SeedAsync(
        IServiceScope rootScope,
        IConfiguration configuration,
        IReadOnlyList<ISeeder> orderedSeeders,
        IReadOnlyList<DemoTenantConfig> tenantConfigs,
        CancellationToken cancellationToken)
    {
        var globalSeeders = orderedSeeders.Where(s => !s.IsTenantScoped).ToList();
        var tenantScopedSeederTypes = orderedSeeders
            .Where(s => s.IsTenantScoped)
            .Select(s => s.GetType())
            .ToList();

        // ─── Pass 1: global Infrastructure seeders against master DB ────────────────
        var rootContext = BuildContext(rootScope, configuration);
        logger.LogInformation("Running {Count} global seeder(s)", globalSeeders.Count);
        foreach (var seeder in globalSeeders)
        {
            await RunSeederAsync(seeder, rootContext, cancellationToken);
        }

        // ─── Pass 2: per-tenant loop ─────────────────────────────────────────────────
        if (tenantConfigs.Count == 0)
        {
            logger.LogWarning("No tenants configured; skipping tenant-scoped seeders");
            return;
        }

        var masterRepo = rootScope.ServiceProvider.GetRequiredService<IMasterUnitOfWork>().Repository<Tenant>();
        var tenantOptions = rootScope.ServiceProvider.GetRequiredService<TenantDbContextOptions>();
        var regionFactory = rootScope.ServiceProvider.GetRequiredService<IRegionProfileFactory>();

        foreach (var tenantConfig in tenantConfigs)
        {
            var tenantEntity = await masterRepo.GetAsync(t => t.Name == tenantConfig.Name, cancellationToken);
            if (tenantEntity is null)
            {
                logger.LogError("Tenant '{Name}' missing from master DB after DemoTenantsSeeder", tenantConfig.Name);
                continue;
            }

            // Switch the singleton options so the next-built TenantDbContext targets this tenant.
            // Source the connection string from the persisted Tenant entity, which holds whatever
            // DemoTenantsSeeder resolved (explicit config or the WithReference fallback).
            tenantOptions.ConnectionString = tenantEntity.ConnectionString;

            using var tenantScope = scopeFactory.CreateScope();
            var tenantContext = BuildContext(tenantScope, configuration);
            tenantContext.CurrentTenant = tenantEntity;
            tenantContext.Region = regionFactory.Get(tenantConfig.Region);
            tenantContext.SeedDataKey = tenantConfig.ResolveSeedDataKey();
            tenantContext.OperatingMode = tenantConfig.OperatingMode;
            tenantContext.DataScale = tenantConfig.DataScale;
            tenantContext.TenantUnitOfWork.SetCurrentTenant(tenantEntity);

            logger.LogInformation("─── Seeding tenant '{Name}' ({Region}) ───",
                tenantEntity.Name, tenantConfig.Region);

            // Resolve seeder instances from the tenant scope so they pick up scoped deps.
            var tenantSeeders = tenantScope.ServiceProvider.GetServices<ISeeder>().ToList();
            var tenantSeederMap = tenantSeeders.ToDictionary(s => s.GetType());

            foreach (var seederType in tenantScopedSeederTypes)
            {
                if (!tenantSeederMap.TryGetValue(seederType, out var seeder))
                {
                    continue;
                }
                await RunSeederAsync(seeder, tenantContext, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Pass 3 - rewrite each tenant's persisted ConnectionString to the host-facing form: same
    /// database name as Passes 1-2 migrated, host/port/credentials from TenantDatabaseDefaults.
    /// </summary>
    private async Task FinalizeConnectionStringsAsync(
        IServiceScope rootScope,
        IConfiguration configuration,
        IReadOnlyList<DemoTenantConfig> tenantConfigs,
        CancellationToken cancellationToken)
    {
        try
        {
            var masterUow = rootScope.ServiceProvider.GetRequiredService<IMasterUnitOfWork>();
            var masterRepo = masterUow.Repository<Tenant>();
            var updated = false;

            foreach (var tenantConfig in tenantConfigs)
            {
                var tenantEntity = await masterRepo.GetAsync(t => t.Name == tenantConfig.Name, cancellationToken);
                if (tenantEntity is null)
                {
                    continue;
                }

                // Keep whatever database name Passes 1-2 actually migrated (a custom name from
                // docker-compose, or the NameTemplate default) - only host/port/credentials flip to
                // the host-facing values, so a renamed database doesn't get silently pointed back at
                // the NameTemplate name that was never created.
                var migratedConnectionString = tenantConfig.ResolveConnectionString(configuration) ?? tenantEntity.ConnectionString;
                var databaseName = new NpgsqlConnectionStringBuilder(migratedConnectionString).Database;
                var hostFacingConnectionString =
                    $"Host={tenantDatabaseDefaults.Host}; Database={databaseName}; Port={tenantDatabaseDefaults.Port}; Username={tenantDatabaseDefaults.UserId}; Password={tenantDatabaseDefaults.Password}";

                if (tenantEntity.ConnectionString != hostFacingConnectionString)
                {
                    tenantEntity.ConnectionString = hostFacingConnectionString;
                    updated = true;
                }
            }

            if (updated)
            {
                await masterUow.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Finalized tenant connection strings to their host-facing form");
            }
        }
        catch (Exception ex)
        {
            // Swallowed so it can't mask the seeder exception that sent us here from the finally.
            logger.LogError(ex, "Could not finalize tenant connection strings to their host-facing form");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private SeederContext BuildContext(IServiceScope scope, IConfiguration configuration)
    {
        return new SeederContext
        {
            IsProduction = environment.IsProduction(),
            ServiceProvider = scope.ServiceProvider,
            Configuration = configuration,
            MasterUnitOfWork = scope.ServiceProvider.GetRequiredService<IMasterUnitOfWork>(),
            TenantUnitOfWork = scope.ServiceProvider.GetRequiredService<ITenantUnitOfWork>(),
            UserManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>(),
            RoleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>()
        };
    }

    private async Task RunSeederAsync(ISeeder seeder, SeederContext context, CancellationToken ct)
    {
        if (await seeder.ShouldSkipAsync(context, ct))
        {
            logger.LogInformation("Skipping {SeederName} (data already exists)", seeder.Name);
            return;
        }

        try
        {
            await seeder.SeedAsync(context, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {SeederName}", seeder.Name);
            throw;
        }
    }
}
