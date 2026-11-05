namespace Trackwise.DbMigrator.Models;

/// <summary>
/// A real driver or helper from the company's Monday.com dispatch boards, seeded by
/// <c>CrewSeeder</c>. Drivers and helpers aren't Monday users, so this is a roster of names the
/// dispatchers picked from - HR/Timemark stays the authority on who they are.
/// </summary>
public record CrewData
{
    public required string FirstName { get; set; }

    /// <summary>Empty when Monday only ever recorded a first name - HR has to complete it.</summary>
    public string LastName { get; set; } = "";

    /// <summary><c>Driver</c> or <c>Helper</c>.</summary>
    public required string Role { get; set; }

    /// <summary><c>Regular</c> or <c>OnCall</c> - an <c>EmploymentType</c> name.</summary>
    public string EmploymentType { get; set; } = "Regular";

    /// <summary><c>ColdChain</c>, <c>WingVan</c> or <c>Trucking</c> - a <c>CrewUnit</c> name, the unit they're usually on.</summary>
    public string Unit { get; set; } = "ColdChain";

    /// <summary>False for crew last seen before the current year: kept so history resolves, not offered for assignment.</summary>
    public bool IsActive { get; set; } = true;
}
