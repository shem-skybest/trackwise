using Trackwise.Domain.Entities;
using Trackwise.Infrastructure.Persistence.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Trackwise.Infrastructure.Persistence.Tests.MasterData;

/// <summary>
/// Employee is the hardest entity to gain <c>ISoftDeletable</c>: its id doubles as a live platform
/// login's id. These pin the same two halves every prior promotion needed - the filter applies to an
/// ordinary query, and a read that must still see a retired employee (payroll history, a delivered
/// load naming its dispatcher) can reach past it - independently of the live-login guard, which is
/// pinned separately in <c>EmployeePermissionResolutionTests</c>.
/// </summary>
public class EmployeeSoftDeleteTests
{
    [Fact]
    public void OrdinaryEmployeeQuery_CarriesTheDeletionPredicate()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Employee>().Where(e => e.IsActive).ToQueryString();

        Assert.Contains("deleted_at", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("IS NULL", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IgnoreQueryFilters_ReachesRetiredEmployees()
    {
        using var db = new TenantDbContext();

        var sql = db.Set<Employee>().IgnoreQueryFilters().ToQueryString();

        // What payroll history or a delivered load naming its dispatcher needs in order to still
        // resolve a since-retired employee's name.
        Assert.DoesNotContain("WHERE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeletingAnEmployee_StampsItRatherThanRemovingTheRow()
    {
        await using var db = MasterDataSaveHarness.Create();

        var employee = NewEmployee();
        db.Add(employee);
        await db.SaveChangesAsync();

        db.Remove(employee);
        await db.SaveChangesAsync();

        // The row has to survive: licenses, time entries and payroll invoices still hold its id as a
        // foreign key, and a hard delete would either fail on that constraint or null out their
        // history.
        Assert.NotNull(employee.DeletedAt);

        // Read off the change tracker rather than re-querying - the in-memory provider cannot
        // materialise the entity's complex properties. A real delete would leave this entry detached.
        Assert.Equal(EntityState.Unchanged, db.Entry(employee).State);
    }

    [Fact]
    public void EmployeeCodeUniqueness_IsScopedToLiveRows()
    {
        using var db = new TenantDbContext();

        var index = db.Model.FindEntityType(typeof(Employee))!
            .GetIndexes()
            .Single(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Employee.Code)));

        // An unfiltered unique index would let a deleted employee hold its code hostage forever, so
        // the code could never be reissued to the record that replaces it.
        Assert.Equal("deleted_at IS NULL", index.GetFilter());
    }

    private static Employee NewEmployee() => new()
    {
        Code = "EMP-001",
        Email = "juan@example.com",
        FirstName = "Juan",
        LastName = "Dela Cruz"
    };
}
