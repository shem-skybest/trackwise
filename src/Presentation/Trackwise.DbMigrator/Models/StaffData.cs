namespace Trackwise.DbMigrator.Models;

/// <summary>
/// A real staff member from the company's Monday.com user export, seeded by <c>StaffSeeder</c>
/// alongside (not instead of) the demo <see cref="UserData" /> roster the fake loads and trips run on.
/// </summary>
public record StaffData
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    /// <summary>A <c>TenantRoles</c> constant; staff seeds never use <c>tenant.driver</c> - drivers aren't Monday users.</summary>
    public required string Role { get; set; }

    /// <summary>Monday job title, seeded into <c>Employee.JobTitle</c>.</summary>
    public string? Title { get; set; }

    public string? PhoneNumber { get; set; }
    public DateTime? JoinedDate { get; set; }
}
