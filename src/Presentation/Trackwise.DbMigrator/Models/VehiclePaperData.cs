namespace Trackwise.DbMigrator.Models;

/// <summary>A registration paper on a seeded truck or trailer: the current LTO registration or an LTFRB franchise.</summary>
public record VehiclePaperData
{
    /// <summary>A <c>VehiclePermitType</c> name: <c>LtoRegistration</c> or <c>LtfrbFranchise</c>.</summary>
    public required string Kind { get; set; }

    public DateOnly? IssuedOn { get; set; }
    public required DateOnly ExpiresOn { get; set; }

    /// <summary>What the last renewal cost, where the register recorded it.</summary>
    public decimal? Amount { get; set; }

    public string? Notes { get; set; }
}
