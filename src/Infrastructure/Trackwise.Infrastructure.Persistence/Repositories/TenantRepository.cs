using Trackwise.Domain.Core;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;
using Trackwise.Infrastructure.Persistence.Repositories;

namespace Trackwise.Infrastructure.Persistence;

internal class TenantRepository<TEntity, TKey> :
    Repository<TenantDbContext, TEntity, TKey>, ITenantRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, ITenantEntity
{
    public TenantRepository(TenantDbContext tenantDbContext) : base(tenantDbContext)
    {
    }
}
