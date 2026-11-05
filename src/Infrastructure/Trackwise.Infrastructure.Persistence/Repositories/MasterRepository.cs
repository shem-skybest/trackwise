using Trackwise.Domain.Core;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;

namespace Trackwise.Infrastructure.Persistence.Repositories;

internal class MasterRepository<TEntity, TKey> :
    Repository<MasterDbContext, TEntity, TKey>, IMasterRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IMasterEntity
{
    public MasterRepository(MasterDbContext masterDbContext) : base(masterDbContext)
    {
    }
}
