using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Infrastructure.Persistence.Conventions;
using Trackwise.Infrastructure.Persistence.Extensions;
using Trackwise.Infrastructure.Persistence.Helpers;
using Trackwise.Infrastructure.Persistence.Interceptors;
using Trackwise.Infrastructure.Persistence.Options;

namespace Trackwise.Infrastructure.Persistence.Data;

public class MasterDbContext : IdentityDbContext<
        User,
        AppRole,
        Guid,
        IdentityUserClaim<Guid>,
        IdentityUserRole<Guid>,
        IdentityUserLogin<Guid>,
        AppRoleClaim,
        IdentityUserToken<Guid>>,
    IDataProtectionKeyContext
{
    private readonly AuditableEntitySaveChangesInterceptor? auditableEntity;
    private readonly string connectionString;
    private readonly DispatchDomainEventsInterceptor? dispatchDomain;
    private readonly SoftDeleteSaveChangesInterceptor? softDelete;
    private readonly ILogger<MasterDbContext>? logger;

    public MasterDbContext(
        MasterDbContextOptions options,
        DispatchDomainEventsInterceptor? dispatchDomain = null,
        AuditableEntitySaveChangesInterceptor? auditableEntity = null,
        ILogger<MasterDbContext>? logger = null,
        SoftDeleteSaveChangesInterceptor? softDelete = null)
    {
        this.dispatchDomain = dispatchDomain;
        this.auditableEntity = auditableEntity;
        this.softDelete = softDelete;
        connectionString = options.ConnectionString ?? ConnectionStrings.LocalMaster;
        this.logger = logger;

        NavigationDiscoveryGuard.Attach(ChangeTracker);
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

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

        // Soft delete only. The change-log interceptor is deliberately absent: MasterDataChangeLog
        // is a tenant entity, so a master-database record would have nowhere to write its history.
        // Give master-database master data a home here before marking any of it ISoftDeletable.
        if (softDelete is not null)
        {
            options.AddInterceptors(softDelete);
        }

        if (!options.IsConfigured)
        {
            DbContextHelpers.ConfigurePostgreSql(connectionString, options);
            logger?.LogInformation("Configured master database with connection string: {ConnectionString}",
                connectionString);
        }
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new SnakeCaseEnumConvention());
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply master configurations & prune tenant types
        builder.PruneTenantOnlyTypesForMaster();

        // Prevent orphaned value objects from remaining in Master model
        builder.Ignore<LaneTransit>();
        builder.Ignore<RouteStop>();
        builder.Ignore<CargoSpecification>();
        builder.Ignore<TemperatureControl>();

        // Issue with EF Core 10: Temporary solution to ignore IdentityPasskeyData entity
        // until proper Passkey support is implemented
        builder.Ignore<IdentityPasskeyData>();

        // Override Identity table names to use snake_case (convention doesn't apply to explicit ToTable calls)
        builder.Entity<User>().ToTable("users");
        builder.Entity<AppRole>().ToTable("roles");
        builder.Entity<AppRoleClaim>().ToTable("role_claims");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        // Scan and apply all configurations from the /Data/Configurations folder
        // for entities implementing IMasterEntity
        builder.ApplyMasterConfigurationsFromAssembly<MasterDbContext>();

        // Prune entity types that are only relevant for the master database
        // It avoids issues with tenant-specific entities being included in the master context and migration errors
        builder.Ignore<BookingInvoice>();
        builder.PruneTenantOnlyTypesForMaster();
    }
}
