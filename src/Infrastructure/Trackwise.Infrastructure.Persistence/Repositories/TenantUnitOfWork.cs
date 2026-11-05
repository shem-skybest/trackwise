using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.Extensions.DependencyInjection;
using Trackwise.Application.Abstractions.Tenancy;

namespace Trackwise.Infrastructure.Persistence;

internal sealed class TenantUnitOfWork(
    TenantDbContext db,
    ICurrentTenantAccessor currentTenantAccessor,
    IMasterUnitOfWork masterUow,
    IServiceProvider services)
    : UnitOfWork<ITenantEntity>(db), ITenantUnitOfWork
{
    private Tenant? currentTenant;

    // Strongly typed repos (hide base with 'new')
    public new ITenantRepository<TEntity, Guid> Repository<TEntity>()
        where TEntity : class, IEntity<Guid>, ITenantEntity
    {
        return (ITenantRepository<TEntity, Guid>)base.Repository<TEntity>();
    }

    public new ITenantRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : class, IEntity<TKey>, ITenantEntity
    {
        return (ITenantRepository<TEntity, TKey>)base.Repository<TEntity, TKey>();
    }

    // Tenant context operations
    public Tenant GetCurrentTenant()
    {
        if (currentTenant is null)
        {
            currentTenant = currentTenantAccessor.GetCurrentTenant();
            db.SwitchToTenant(currentTenant);
        }

        return currentTenant;
    }

    public async Task<Tenant> SetCurrentTenantByIdAsync(Guid tenantId)
    {
        var tenant = await masterUow.Repository<Tenant>().GetByIdAsync(tenantId);
        currentTenant = tenant ?? throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");

        db.SwitchToTenant(currentTenant);
        return currentTenant;
    }

    public void SetCurrentTenant(Tenant tenant)
    {
        currentTenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
        db.SwitchToTenant(currentTenant);
    }

    // Otherwise the transaction opens on the default database and the first repository call inside
    // it fails trying to switch the now-open connection to the tenant's.
    protected override void PrepareConnection()
    {
        _ = GetCurrentTenant();
    }

    protected override IRepository<TEntity, TKey> CreateRepository<TEntity, TKey>()
    {
        // First set the current tenant if not already set
        _ = GetCurrentTenant();

        // TenantRepository<TEntity, TKey>(TenantDbContext db)
        return ActivatorUtilities.CreateInstance<TenantRepository<TEntity, TKey>>(services, db);
    }
}
