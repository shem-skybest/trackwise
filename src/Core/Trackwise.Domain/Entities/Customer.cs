using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Company's customer (e.g. broker, shipper, etc.).
/// </summary>
public class Customer : AuditableEntity, ITenantEntity
{
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? Address { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public string? Notes { get; set; }

    /// <summary>
    /// VAT ID (EU), EIN (US), GSTIN (IN), ABN (AU), etc. Required for EU B2B reverse-charge.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// When true, the tax calculator skips computing tax for this customer (charity, gov, NGO).
    /// </summary>
    public bool IsVatExempt { get; set; }

    /// <summary>The client's billing cut-off, which sets the period of each billing batch. Null when not agreed.</summary>
    public BillingCycle? BillingCycle { get; set; }

    public virtual List<LoadInvoice> Invoices { get; set; } = [];
}
