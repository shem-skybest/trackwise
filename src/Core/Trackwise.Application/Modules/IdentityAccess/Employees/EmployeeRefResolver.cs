using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.IdentityAccess.Employees;

/// <summary>
/// Batches the employee name a page of records displays, in one query.
/// <para>
/// Two separate reasons this cannot be a navigation property. Lazy loading is on, so reading one
/// per row is an N+1 the mapper hides; and employees are soft-deletable, so the global query filter
/// would return null for an employee who has since been retired - blanking (or NRE-ing, since
/// <c>TimeEntry.Employee</c> / <c>PayrollInvoice.Employee</c> are non-nullable navigations) the name
/// on a time entry or payroll invoice that genuinely involved them. Display lookups therefore read
/// past the filter: who a record was for is a fact about history, not a record a steward is being
/// offered.
/// </para>
/// </summary>
internal static class EmployeeRefResolver
{
    public static async Task<IReadOnlyDictionary<Guid, EmployeeRef>> ResolveAsync(
        ITenantUnitOfWork uow,
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken ct = default)
    {
        if (employeeIds.Count == 0)
        {
            return new Dictionary<Guid, EmployeeRef>();
        }

        return await uow.Repository<Employee>().Query()
            .IgnoreQueryFilters()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => new EmployeeRef(e.GetFullName()), ct);
    }

    /// <summary>Single-id convenience wrapper for by-id handlers.</summary>
    public static async Task<string?> ResolveNameAsync(
        ITenantUnitOfWork uow, Guid? employeeId, CancellationToken ct = default)
    {
        if (employeeId is not { } id)
        {
            return null;
        }

        var refs = await ResolveAsync(uow, [id], ct);
        return refs.TryGetValue(id, out var employeeRef) ? employeeRef.Name : null;
    }

    /// <summary>
    /// Full entities for a caller building a complete <c>EmployeeDto</c> - an embedded payroll
    /// invoice employee, for instance - rather than just a display name.
    /// </summary>
    public static async Task<IReadOnlyDictionary<Guid, Employee>> ResolveEntitiesAsync(
        ITenantUnitOfWork uow,
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken ct = default)
    {
        if (employeeIds.Count == 0)
        {
            return new Dictionary<Guid, Employee>();
        }

        return await uow.Repository<Employee>().Query()
            .IgnoreQueryFilters()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);
    }
}
