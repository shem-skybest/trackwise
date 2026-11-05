using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;
using Trackwise.Infrastructure.Persistence.Options;
using Trackwise.Infrastructure.Persistence.Repositories;
using Trackwise.Infrastructure.Persistence.Services;
using Trackwise.Infrastructure.Persistence.Services.AIDispatch;
using Trackwise.Infrastructure.Persistence.Services.Feature;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Features;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Abstractions.Modules.Platform.ReadModels;
using Trackwise.Application.Abstractions.SystemSettings;
using Trackwise.Application.Abstractions.Tenancy;
using Trackwise.Application.Abstractions.AIDispatch;
using Trackwise.Infrastructure.Persistence.Reads.Platform;
using Trackwise.Infrastructure.Persistence.Services.FreightForwarding;
using Trackwise.Infrastructure.Persistence.Services.MasterData;
using Trackwise.Application.Abstractions.Modules.MasterData;

namespace Trackwise.Infrastructure.Persistence.Builder;

internal sealed class PersistenceInfrastructureBuilder : IPersistenceInfrastructureBuilder
{
    private readonly IConfiguration configuration;
    private readonly IServiceCollection services;
    private ILogger<IPersistenceInfrastructureBuilder>? logger;

    internal PersistenceInfrastructureBuilder(IServiceCollection services, IConfiguration configuration)
    {
        this.configuration = configuration;
        this.services = services;
    }

    public IPersistenceInfrastructureBuilder AddIdentity(Action<IdentityBuilder>? configure = null)
    {
        var identityBuilder = services.AddIdentityCore<User>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.AllowedUserNameCharacters =
                    "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789_.@-+";
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<MasterDbContext>();

        configure?.Invoke(identityBuilder);
        return this;
    }

    public IPersistenceInfrastructureBuilder UseLogger(ILogger<IPersistenceInfrastructureBuilder> infrastructureLogger)
    {
        logger = infrastructureLogger;
        return this;
    }

    public IPersistenceInfrastructureBuilder AddMasterDatabase(Action<MasterDbContextOptions>? configure = null)
    {
        var options = new MasterDbContextOptions();
        configure?.Invoke(options);

        var connectionString = configuration.GetConnectionString(options.DbConnectionSection);
        options.ConnectionString = connectionString;

        services.AddSingleton(options);
        services.AddDbContext<MasterDbContext>();
        services.AddScoped<IMasterUnitOfWork, MasterUnitOfWork>();
        services.AddScoped(typeof(MasterRepository<,>));
        services.AddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();
        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IAIQuotaService, AIQuotaService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        logger?.LogInformation("Added master database with connection string: {ConnectionString}", connectionString);
        return this;
    }

    public IPersistenceInfrastructureBuilder AddTenantDatabase(Action<TenantDbContextOptions>? configure = null)
    {
        var options = new TenantDbContextOptions();
        configure?.Invoke(options);

        var tenantsSettings =
            configuration.GetSection(TenantDatabaseDefaults.SectionName).Get<TenantDatabaseDefaults>();
        var connectionString = configuration.GetConnectionString(options.DefaultTenantDbConnectionSection);

        if (tenantsSettings is not null)
        {
            services.AddScoped<ITenantDatabaseService, TenantDatabaseService>();
            services.AddSingleton(tenantsSettings);
            logger?.LogInformation("Tenants database settings: {Settings}", tenantsSettings);
        }

        options.ConnectionString = connectionString;
        services.AddSingleton(options);
        services.AddDbContext<TenantDbContext>();
        services.AddScoped<ITenantUnitOfWork, TenantUnitOfWork>();
        services.AddScoped(typeof(TenantRepository<,>));
        services.AddScoped<ISafetyReportReader, SafetyReportReader>();
        services.AddScoped<IWorkfileReferenceGenerator, WorkfileReferenceGenerator>();
        services.AddScoped<IMasterDataCodeGenerator, MasterDataCodeGenerator>();
        services.AddScoped<IDuplicateProbe, DuplicateProbe>();
        services.AddScoped<IDependencyProbe, DependencyProbe>();
        services.AddScoped<IMasterDataReferenceRepointer, MasterDataReferenceRepointer>();
        logger?.LogInformation("Added default tenant database with connection string: {ConnectionString}",
            connectionString);
        return this;
    }
}
