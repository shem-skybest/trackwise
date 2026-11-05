using Trackwise.Infrastructure.Persistence.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Trackwise.Infrastructure.Persistence.Builder;

public interface IPersistenceInfrastructureBuilder
{
    IPersistenceInfrastructureBuilder AddIdentity(Action<IdentityBuilder>? configure = null);
    IPersistenceInfrastructureBuilder AddMasterDatabase(Action<MasterDbContextOptions>? configure = null);
    IPersistenceInfrastructureBuilder AddTenantDatabase(Action<TenantDbContextOptions>? configure = null);
    IPersistenceInfrastructureBuilder UseLogger(ILogger<IPersistenceInfrastructureBuilder> infrastructureLogger);
}
