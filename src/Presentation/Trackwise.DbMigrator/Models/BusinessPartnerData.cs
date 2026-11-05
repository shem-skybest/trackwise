namespace Trackwise.DbMigrator.Models;

/// <summary>
/// A real counterparty from the company's Monday.com operation boards (carriers, container yards,
/// agents, brokers, shippers, consignees), seeded by <c>BusinessPartnerSeeder</c> in place of its
/// placeholder partners.
/// </summary>
public record BusinessPartnerData
{
    public required string Name { get; set; }

    /// <summary><c>BusinessPartnerRole</c> names, e.g. <c>ShippingLine</c>, <c>Depot</c>, <c>Consignee</c>.</summary>
    public required string[] Roles { get; set; }

    /// <summary>The spellings the Monday boards used for this partner - kept in the partner's notes for matching.</summary>
    public string[] Aliases { get; set; } = [];

    /// <summary>Name of an existing seeded <c>Customer</c> that is the same company, to link the two.</summary>
    public string? CustomerName { get; set; }

    public string? Note { get; set; }
}
