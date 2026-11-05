using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.DbMigrator.Data;

internal class CreateSqlFunctionsWorker(
    ILogger<CreateSqlFunctionsWorker> logger,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();

        var tenants = await masterDb.Set<Tenant>()
            .Where(t => t.ConnectionString != null && t.ConnectionString != "")
            .Select(t => new { t.Name, t.ConnectionString })
            .ToListAsync(cancellationToken);

        logger.LogInformation("Creating SQL functions for {Count} tenant database(s)", tenants.Count);

        // See MigrateDatabaseWorker for why demo tenants resolve their own connection string here
        // instead of always using the persisted Tenant.ConnectionString.
        var demoTenantConfigs = configuration.GetSection("Tenants").Get<List<DemoTenantConfig>>() ?? [];

        foreach (var tenant in tenants)
        {
            var demoConfig = demoTenantConfigs.Find(c => c.Name == tenant.Name);
            var connectionString = demoConfig?.ResolveConnectionString(configuration) ?? tenant.ConnectionString;

            try
            {
                using var tenantScope = scopeFactory.CreateScope();
                var tenantDb = tenantScope.ServiceProvider.GetRequiredService<TenantDbContext>();
                tenantDb.Database.SetConnectionString(connectionString);

                logger.LogInformation("Creating SQL functions for tenant '{TenantName}'...", tenant.Name);
                await CreateSqlFunction("CreateCompanyStats.psql", tenantDb);
                await CreateSqlFunction("CreateTrucksStats.psql", tenantDb);
                logger.LogInformation("SQL functions created for tenant '{TenantName}'", tenant.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create SQL functions for tenant '{TenantName}'", tenant.Name);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task CreateSqlFunction(string fileName, TenantDbContext db)
    {
        var sql = await File.ReadAllTextAsync($"SqlFunctions/{fileName}");
        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
