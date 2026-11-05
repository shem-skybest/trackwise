using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.Persistence.Conventions;
using Trackwise.Infrastructure.Persistence.Extensions;
using Trackwise.Infrastructure.Persistence.Helpers;
using Trackwise.Infrastructure.Persistence.Interceptors;
using Trackwise.Infrastructure.Persistence.Options;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Persistence.Data;

public class TenantDbContext : DbContext
{
    private readonly AuditableEntitySaveChangesInterceptor? auditableEntity;

    // Default fallback connection string for local development and testing
    private readonly string defaultConnectionString;

    private readonly DispatchDomainEventsInterceptor? dispatchDomain;
    private readonly SoftDeleteSaveChangesInterceptor? softDelete;
    private readonly ChangeLogSaveChangesInterceptor? changeLog;
    private readonly IDataProtectionProvider? dataProtectionProvider;
    private readonly ILogger<TenantDbContext>? logger;

    public TenantDbContext(
        TenantDbContextOptions? tenantDbContextOptions = null,
        DispatchDomainEventsInterceptor? dispatchDomain = null,
        AuditableEntitySaveChangesInterceptor? auditableEntity = null,
        IDataProtectionProvider? dataProtectionProvider = null,
        ILogger<TenantDbContext>? logger = null,
        SoftDeleteSaveChangesInterceptor? softDelete = null,
        ChangeLogSaveChangesInterceptor? changeLog = null)
    {
        this.dispatchDomain = dispatchDomain;
        this.auditableEntity = auditableEntity;
        this.softDelete = softDelete;
        this.changeLog = changeLog;
        this.dataProtectionProvider = dataProtectionProvider;
        this.logger = logger;

        defaultConnectionString = tenantDbContextOptions?.ConnectionString
                                  ?? ConnectionStrings.LocalDefaultTenant;

        NavigationDiscoveryGuard.Attach(ChangeTracker);
    }

    /// <summary>
    ///     Switch the underlying connection to a tenant-specific database.
    ///     Call this BEFORE the first query or SaveChanges.
    /// </summary>
    /// <param name="tenant">The tenant to switch to, containing the connection string. </param>
    public void SwitchToTenant(Tenant tenant)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenant.ConnectionString);
        Database.SetConnectionString(tenant.ConnectionString); // EF Core runtime retargeting
        logger?.LogInformation("Switched tenant database to '{TenantName}'.", tenant.Name);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (dispatchDomain is not null)
        {
            options.AddInterceptors(dispatchDomain);
        }

        if (auditableEntity is not null)
        {
            options.AddInterceptors(auditableEntity);
        }

        // Registration order is the execution order, and these two are ordered on purpose: soft
        // delete rewrites a Deleted entry into a DeletedAt transition, which the change log then
        // reads to record a deletion rather than an edit.
        if (softDelete is not null)
        {
            options.AddInterceptors(softDelete);
        }

        if (changeLog is not null)
        {
            options.AddInterceptors(changeLog);
        }

        if (!options.IsConfigured)
        {
            DbContextHelpers.ConfigurePostgreSql(defaultConnectionString, options);
            logger?.LogInformation("Configured tenant database with default connection string.");
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new SnakeCaseEnumConvention());
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Scan and apply all configurations from the /Data/Configurations folder
        // for entities implementing ITenantEntity
        builder.ApplyTenantConfigurationsFromAssembly<TenantDbContext>();

        // Prune master-only types
        builder.PruneMasterOnlyTypesForTenant();

        // Encrypt provider-secret columns at rest (ELD / LoadBoard / Accounting). Applied after
        // entity configs so it overrides their plain string mappings. No-op when no protector.
        builder.ApplyEncryptedSecretColumns(dataProtectionProvider);

        // Query-only DTOs for PostgreSQL functions - no table generation
        builder.Entity<CompanyStatsDto>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable((string?)null);
        });

        builder.Entity<TruckStatsDto>(entity =>
        {
            entity.HasNoKey();
            entity.ToTable((string?)null);

            entity.Property(t => t.Drivers)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<EmployeeDto>>(v, (JsonSerializerOptions)null));
        });
    }
}
