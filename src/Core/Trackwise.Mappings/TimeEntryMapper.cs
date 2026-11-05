using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class TimeEntryMapper
{
    /// <summary>
    /// Adds the employee display name from a lookup the caller has already batched past the
    /// soft-delete filter. Lazy loading is on, and <c>Employee</c> is soft-deletable, so reading
    /// <c>entity.Employee</c> here would issue a query per row and NRE for a retired employee.
    /// </summary>
    public static TimeEntryDto ToDto(this TimeEntry entity, string? employeeName)
    {
        var dto = entity.ToDto();
        dto.EmployeeName = employeeName;
        return dto;
    }

    public static TimeEntryDto ToDto(this TimeEntry entity)
    {
        return new TimeEntryDto
        {
            Id = entity.Id.ToString(),
            EmployeeId = entity.EmployeeId.ToString(),
            Date = entity.Date,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            TotalHours = entity.TotalHours,
            Type = entity.Type.ToString().ToLowerInvariant(),
            PayrollInvoiceId = entity.PayrollInvoiceId?.ToString(),
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt
        };
    }
}
