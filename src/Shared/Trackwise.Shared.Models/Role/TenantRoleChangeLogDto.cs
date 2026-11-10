namespace Trackwise.Shared.Models;

public record TenantRoleChangeLogDto
{
    public Guid Id { get; init; }
    public required string ChangeType { get; init; }
    public required string RoleName { get; init; }
    public string? RoleDisplayName { get; init; }
    public string? Permission { get; init; }
    public Guid? EmployeeId { get; init; }
    public string? EmployeeName { get; init; }
    public string? PreviousRoleName { get; init; }
    public string? ChangedByName { get; init; }
    public DateTime ChangedAt { get; init; }
}
