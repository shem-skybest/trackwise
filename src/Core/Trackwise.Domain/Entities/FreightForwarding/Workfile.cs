using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
// See Workfile.Genset.cs for why this alias exists - GensetBillingType the property shadows
// GensetBillingType the type in expression position.
using GensetBillingKind = Trackwise.Domain.Primitives.Enums.GensetBillingType;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The post-booking operational and pre-billing file for a shipment: where ops track containers and
/// execution, accumulate selling and buying charges, route the file for approval, and finally
/// forward finalised charges to Accounting. Flow: Booking -> Workfile -> Billing.
/// <para>
/// Abstract on purpose. Regular (ocean), Reefer and Trucking files differ in fields, reference
/// series and required-field rules, so the type is a real TPH discriminator and the per-type rules
/// are abstract members below - no handler branches on a reference-number prefix.
/// </para>
/// </summary>
public abstract partial class Workfile : AuditableEntity, ITenantEntity
{
    /// <summary>Discriminator. Each subclass pins its own value.</summary>
    public abstract WorkfileType Type { get; protected set; }

    /// <summary>
    /// Human-facing file number, e.g. "BKN-IMP-000123". Allocated from <see cref="ReferenceSeries"/>
    /// on creation unless the caller supplied an override. Unique per tenant.
    /// </summary>
    public string ReferenceNumber { get; private set; } = default!;

    /// <summary>The series this file's reference is drawn from, e.g. "BKN-IMP".</summary>
    public abstract string ReferenceSeries { get; }

    // --- Per-type rules. Delegated to WorkfileRules so a caller with only a WorkfileType and a
    // caller with a loaded file always get the same answer. ---

    public WorkfileRules Rules => WorkfileRules.For(Type);

    public bool RequiresBookingNumber => Rules.RequiresBookingNumber;
    public bool RequiresBillIndicator => Rules.RequiresBillIndicator;
    public bool RequiresDuplicateBlCheck => Rules.RequiresDuplicateBlCheck;
    public bool RequiresDeliveryLocation => Rules.RequiresDeliveryLocation;
    public ContainerKind ContainerKind => Rules.ContainerKind;
    public bool SupportsGarageTracking => Rules.SupportsGarageTracking;
    public bool SupportsBudgetDistribution => Rules.SupportsBudgetDistribution;
    public bool SupportsGenset => Rules.SupportsGenset;

    /// <summary>
    /// Whether billing-forward must ask the user to choose between zero-rated and VAT-exempt.
    /// Direction-dependent, so unlike the rules above this one stays polymorphic.
    /// </summary>
    public abstract bool PromptsForVatTreatment { get; }

    // --- Shipment identity ---

    public string? BillOfLadingNumber { get; private set; }

    public Guid? BookingId { get; private set; }
    public virtual Booking? Booking { get; private set; }

    public Guid CustomerId { get; private set; }

    public BillIndicator? BillIndicator { get; private set; }

    public string? VesselName { get; private set; }
    public string? VoyageNumber { get; private set; }
    public DateTime? Etd { get; private set; }
    public DateTime? Eta { get; private set; }
    public string? Origin { get; private set; }
    public string? Destination { get; private set; }
    public string? Commodity { get; private set; }

    // --- Counterparties. Each must be active and hold the matching role (BusinessPartner.CanActAs). ---

    public Guid? ShippingLineId { get; private set; }
    public virtual BusinessPartner? ShippingLine { get; private set; }

    public Guid? CoLoaderId { get; private set; }
    public virtual BusinessPartner? CoLoader { get; private set; }

    public Guid? ForwarderId { get; private set; }
    public virtual BusinessPartner? Forwarder { get; private set; }

    public Guid? TruckerId { get; private set; }
    public virtual BusinessPartner? Trucker { get; private set; }

    public Guid? BrokerId { get; private set; }
    public virtual BusinessPartner? Broker { get; private set; }

    // --- Commercial ---

    public int? CreditTermDays { get; private set; }
    public CreditType? CreditType { get; private set; }

    /// <summary>Rate agreed when the file was opened. Entered by hand - there is no FX service.</summary>
    public decimal? ExchangeRate { get; private set; }

    /// <summary>Rate actually settled at, known only once the file is billed.</summary>
    public decimal? FinalExchangeRate { get; private set; }

    public string? TaggingCategory { get; private set; }
    public bool IsAdvanceBilling { get; private set; }
    public ReportCategory ReportCategory { get; private set; }

    // --- Execution ---

    public DateTimeOffset? GarageDepartureAt { get; private set; }
    public DateTimeOffset? GarageArrivalAt { get; private set; }

    // --- Genset add-on. Reefer-only (see WorkfileRules.SupportsGenset) - never seen booked
    // standalone, always attached to a reefer container job. See Workfile.Genset.cs. ---

    public Guid? GensetId { get; private set; }
    public virtual Genset? Genset { get; private set; }
    public GensetBillingType? GensetBillingType { get; private set; }
    public decimal? GensetHourlyRate { get; private set; }
    public decimal? GensetHoursUsed { get; private set; }
    public decimal? GensetFlatAmount { get; private set; }
    public string? GensetNotes { get; private set; }

    /// <summary>
    /// Itemized: hourly rate x hours actually used. ProfitShare: the flat amount, regardless of
    /// hours - a co-loader/partner is fronting the job so no per-hour math applies. Null until a
    /// genset add-on exists at all.
    /// </summary>
    public decimal? GensetTotalCharge => GensetBillingType switch
    {
        GensetBillingKind.Itemized => GensetHourlyRate * GensetHoursUsed,
        GensetBillingKind.ProfitShare => GensetFlatAmount,
        _ => null
    };

    // --- Hand-off state ---

    public WorkfileStatus Status { get; private set; }

    public bool IsForwardedToAccounting { get; private set; }
    public DateTimeOffset? ForwardedToAccountingAt { get; private set; }
    public string? AccountingRemarks { get; private set; }

    /// <summary>
    /// Set while a reference-closure request is open. Orthogonal to <see cref="Status"/>: a locked
    /// file refuses every write regardless of where it sits in the lifecycle, and only Accounting
    /// can release it.
    /// </summary>
    public bool IsReferenceLocked { get; private set; }

    public string? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
}
