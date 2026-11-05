using Trackwise.Infrastructure.Persistence.Data;
using Trackwise.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// A <see cref="TenantDbContext" /> pinned to an in-memory store so the save pipeline runs without
/// a database.
/// <para>
/// The provider is chosen first and the base <c>OnConfiguring</c> called second: the base only
/// falls back to PostgreSQL when the options are still unconfigured, so this both keeps the real
/// interceptor wiring under test and keeps the test offline. What is being asserted - that a
/// delete becomes a soft delete and that a save leaves a field-level trail - is provider-agnostic.
/// </para>
/// </summary>
internal sealed class MasterDataSaveHarness(
    string storeName,
    SoftDeleteSaveChangesInterceptor? softDelete = null,
    ChangeLogSaveChangesInterceptor? changeLog = null,
    ModifiedPropertyCapturingInterceptor? observer = null)
    : TenantDbContext(softDelete: softDelete, changeLog: changeLog)
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseInMemoryDatabase(storeName);
        base.OnConfiguring(options);

        // Last, so it sees the state the interceptors under test leave behind.
        if (observer is not null)
        {
            options.AddInterceptors(observer);
        }
    }

    /// <summary>A harness with both master-data interceptors wired, as production has them.</summary>
    public static MasterDataSaveHarness Create(ModifiedPropertyCapturingInterceptor? observer = null) =>
        new(Guid.NewGuid().ToString(),
            new SoftDeleteSaveChangesInterceptor(),
            new ChangeLogSaveChangesInterceptor(),
            observer);
}
