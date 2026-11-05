namespace Trackwise.DbMigrator.Models;

/// <summary>Fuel on top of a seeded rate line at one diesel price tier.</summary>
public record RateSurchargeData
{
    public decimal DieselPrice { get; set; }
    public decimal Amount { get; set; }
}
