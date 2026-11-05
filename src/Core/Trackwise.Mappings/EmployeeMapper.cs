using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class EmployeeMapper
{
    public static EmployeeDto ToDto(this Employee entity)
    {
        return new EmployeeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Email = entity.Email,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            FullName = entity.GetFullName(),
            PhoneNumber = entity.PhoneNumber,
            Address = entity.Address,
            JoinedDate = entity.JoinedDate,
            Salary = entity.Salary,
            SalaryType = entity.SalaryType,
            Status = entity.Status,
            StripeConnectedAccountId = entity.StripeConnectedAccountId,
            Role = entity.Role?.ToDto(),
            JobTitle = entity.JobTitle,
            EmploymentType = entity.EmploymentType,
            HomeUnit = entity.HomeUnit,
            BranchId = entity.BranchId,
            IsActive = entity.IsActive,
            MergedIntoId = entity.MergedIntoId,
            DeletedAt = entity.DeletedAt,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedAt = entity.UpdatedAt,
            UpdatedBy = entity.UpdatedBy
        };
    }

    /// <summary>
    /// Adds the owning branch name from a lookup the caller has already batched. Lazy loading is on,
    /// so reading <c>entity.Branch</c> here would issue a query per row.
    /// </summary>
    public static EmployeeDto ToDto(
        this Employee entity, IReadOnlyDictionary<Guid, string> branchNames)
    {
        var dto = entity.ToDto();

        if (entity.BranchId is { } branchId && branchNames.TryGetValue(branchId, out var name))
        {
            dto.BranchName = name;
        }

        return dto;
    }
}
