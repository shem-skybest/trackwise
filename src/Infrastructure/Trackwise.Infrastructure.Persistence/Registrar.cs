using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Abstractions.Privacy;
using Trackwise.Domain.Options;
using Trackwise.Infrastructure.Persistence.Builder;
using Trackwise.Infrastructure.Persistence.Interceptors;
using Trackwise.Infrastructure.Persistence.Privacy;
using Trackwise.Infrastructure.Persistence.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Infrastructure.Persistence;

public static class Registrar
{
    /// <summary>
    ///     Add a persistence infrastructure layer (databases, repositories, multi-tenancy).
    ///     Returns IInfrastructureBuilder for fluent configuration of Master/Tenant databases.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The infrastructure builder.</returns>
    public static IPersistenceInfrastructureBuilder AddPersistenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<DispatchDomainEventsInterceptor>());

        // Master-data entity policy: pure configuration with no dependencies, registered here
        // because both the Application pipeline and the Infrastructure probe need it, and
        // Infrastructure must never reference Application.
        services.TryAddSingleton<IMasterDataPolicyProvider, MasterDataPolicyProvider>();

        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        // Order matters at save time: soft delete rewrites the deletion before the change log
        // classifies it. See the interceptors' remarks.
        services.AddScoped<SoftDeleteSaveChangesInterceptor>();
        services.AddScoped<ChangeLogSaveChangesInterceptor>();

        services.Configure<CustomerPortalOptions>(configuration.GetSection(CustomerPortalOptions.SectionName));
        services.Configure<IdentityServerOptions>(configuration.GetSection(IdentityServerOptions.SectionName));

        // Current user service (only for web apps with IHttpContextAccessor)
        services.TryAddScoped<ICurrentUserService>(sp =>
        {
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            return httpContextAccessor is not null
                ? new CurrentUserService(httpContextAccessor)
                : new NoopCurrentUserService();
        });

        // Privacy / GDPR
        services.AddScoped<IDataAnonymizer, DataAnonymizer>();

        return new PersistenceInfrastructureBuilder(services, configuration);
    }
}
