using Trackwise.Application.Abstractions.Tenancy;
namespace Trackwise.Application.Abstractions.Tenancy;

public interface ITenantDatabaseService
{
    string GenerateConnectionString(string tenantName);
    Task<bool> CreateDatabaseAsync(string connectionString);
    Task<bool> DeleteDatabaseAsync(string connectionString);
}
