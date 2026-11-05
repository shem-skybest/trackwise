using Microsoft.EntityFrameworkCore.Design;

namespace Trackwise.Infrastructure.Persistence.Data;

public class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args) => new();
}
