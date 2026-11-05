using Duende.IdentityServer.EntityFramework.DbContexts;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.DbMigrator.Workers;

public class MigrateDatabaseWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MigrateDatabaseWorker> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        logger.LogInformation("Applying migrations to Master database...");
        await masterDb.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Master database migrated successfully");

        var grantDb = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
        logger.LogInformation("Applying migrations to Duende operational store (master DB)...");
        await grantDb.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Duende operational store migrated successfully");

        // Query all tenants with valid connection strings from the master database
        var tenants = await masterDb.Set<Tenant>()
            .Where(t => t.ConnectionString != null && t.ConnectionString != "")
            .Select(t => new { t.Name, t.ConnectionString })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} tenant database(s) to migrate", tenants.Count);

        // Demo tenants (Tenants[] config) may have a distinct connection string for the migrator's
        // own reach - e.g. the Docker-internal "postgres" host in local dev - which can differ from
        // the persisted Tenant.ConnectionString once SeederOrchestrationWorker's Pass 3 has rewritten
        // it to the host-facing form for other services. Real, live-created tenants aren't in this
        // config at all, so they fall back to the persisted value as before.
        var demoTenantConfigs = configuration.GetSection("Tenants").Get<List<DemoTenantConfig>>() ?? [];

        // Migrate each tenant database individually
        var failedTenants = new List<string>();
        foreach (var tenant in tenants)
        {
            var demoConfig = demoTenantConfigs.Find(c => c.Name == tenant.Name);
            var connectionString = demoConfig?.ResolveConnectionString(configuration) ?? tenant.ConnectionString;

            try
            {
                using var tenantScope = scopeFactory.CreateScope();
                var tenantDb = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();
                tenantDb.Database.SetConnectionString(connectionString);

                logger.LogInformation("Applying migrations to tenant '{TenantName}'...", tenant.Name);
                await tenantDb.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Tenant '{TenantName}' migrated successfully", tenant.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to migrate tenant '{TenantName}'", tenant.Name);
                failedTenants.Add(tenant.Name);
            }
        }

        if (failedTenants.Count > 0)
        {
            logger.LogError("Migration failed for {Count} tenant(s): {Tenants}",
                failedTenants.Count, string.Join(", ", failedTenants));

            // The other tenants still migrate, but the run must not look green to CI or a deploy.
            Environment.ExitCode = 1;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
