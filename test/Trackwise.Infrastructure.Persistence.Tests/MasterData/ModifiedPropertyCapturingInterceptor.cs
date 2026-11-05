using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// Records which properties were dirty at save time, so a test can assert what a save would
/// actually write. Registered after the interceptor under test, since what matters is the state it
/// leaves behind - and by the time <c>SaveChanges</c> returns, EF has already reset the flags.
/// </summary>
internal sealed class ModifiedPropertyCapturingInterceptor : SaveChangesInterceptor
{
    public List<string> ModifiedProperties { get; } = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        ModifiedProperties.Clear();

        ModifiedProperties.AddRange(context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified)
            .SelectMany(e => e.Properties)
            .Where(p => p.IsModified)
            .Select(p => p.Metadata.Name));
    }
}
