using System.Linq.Expressions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Specifications;

namespace Trackwise.Application.Modules.Financial.Invoices.Specifications;

/// <summary>
/// Specification for filtering payroll invoices with comprehensive filter options.
/// </summary>
public sealed class FilterPayrollInvoices : BaseSpecification<PayrollInvoice>
{
    /// <param name="matchingEmployeeIds">
    /// Ids of employees matching <paramref name="employeeName" />/<paramref name="salaryType" />,
    /// resolved by the caller past the soft-delete filter - see
    /// <see cref="Trackwise.Application.Modules.IdentityAccess.Employees.EmployeeRefResolver" />.
    /// A plain <c>i.Employee.FirstName</c> criteria here would translate to a join honoring
    /// Employee's global query filter and silently drop every retired employee's payroll history
    /// the moment either filter is used. Null when neither filter was requested.
    /// </param>
    public FilterPayrollInvoices(
        Guid? employeeId,
        string? employeeName,
        InvoiceStatus? status,
        SalaryType? salaryType,
        DateTime? startDate,
        DateTime? endDate,
        IReadOnlyCollection<Guid>? matchingEmployeeIds,
        string? orderBy,
        int page,
        int pageSize)
    {
        Expression<Func<PayrollInvoice, bool>>? criteria = null;

        // Filter by Employee ID
        if (employeeId.HasValue)
        {
            criteria = AddCriteria(criteria, i => i.EmployeeId == employeeId.Value);
        }

        // Filter by Employee Name and/or Salary Type: resolved to a set of ids by the caller so a
        // retired employee's payroll invoices are found instead of silently dropped.
        if (!string.IsNullOrWhiteSpace(employeeName) || salaryType.HasValue)
        {
            var ids = matchingEmployeeIds ?? [];
            criteria = AddCriteria(criteria, i => ids.Contains(i.EmployeeId));
        }

        // Filter by Status
        if (status.HasValue)
        {
            criteria = AddCriteria(criteria, i => i.Status == status.Value);
        }

        // Filter by date range (based on PeriodStart/PeriodEnd)
        if (startDate.HasValue)
        {
            criteria = AddCriteria(criteria, i => i.PeriodEnd >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            criteria = AddCriteria(criteria, i => i.PeriodStart <= endDate.Value);
        }

        if (criteria is not null)
        {
            Criteria = criteria;
        }

        OrderBy(orderBy);
        ApplyPaging(page, pageSize);
    }

    private static Expression<Func<PayrollInvoice, bool>> AddCriteria(
        Expression<Func<PayrollInvoice, bool>>? existing,
        Expression<Func<PayrollInvoice, bool>> newCriteria)
    {
        if (existing is null)
        {
            return newCriteria;
        }

        // Combine expressions with AND
        var parameter = Expression.Parameter(typeof(PayrollInvoice), "i");
        var combined = Expression.AndAlso(
            Expression.Invoke(existing, parameter),
            Expression.Invoke(newCriteria, parameter));

        return Expression.Lambda<Func<PayrollInvoice, bool>>(combined, parameter);
    }
}
