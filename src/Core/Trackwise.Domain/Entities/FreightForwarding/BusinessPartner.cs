using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A trading counterparty on a freight-forwarding file - shipping line, co-loader, forwarder,
/// trucker, broker, agent, shipper, consignee or depot.
/// <para>
/// Distinct from <see cref="Customer"/>, which is who gets billed. The shipper on a file is often
/// not the paying customer, and a single partner can act in several roles at once.
/// </para>
/// <para>
/// The module's first record that <b>cannot go live on one person's say-so</b>. A wrong port costs
/// a correction; a wrong partner carries a TIN onto a forwarded charge and an address onto a bill
/// of lading, so <see cref="IMasterDataApprovable"/> is what makes this entity different from the
/// reference lists rather than merely later than them.
/// </para>
/// </summary>
public class BusinessPartner
    : AuditableEntity, ITenantEntity, IMasterDataRecord, IMasterDataApprovable, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.BusinessPartner;

    public required string Name { get; set; }

    /// <summary>
    /// Short tenant-unique identifier (e.g. "MAEU", "ICTSI").
    /// <para>
    /// Canonicalised on write by <see cref="NormalizeCode"/>, so callers that query by code must
    /// normalise the search term through it - the stored value is always canonical.
    /// </para>
    /// <para>
    /// Blank on create means "allocate from the <c>BusinessPartner</c> number series", and the code
    /// is immutable afterwards like every master code: it is the handle the accounting database and
    /// nSFS join on, so a rename would strand every legacy key pointing here.
    /// </para>
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <inheritdoc cref="Code"/>
    public static string NormalizeCode(string? value) =>
        value?.Trim().ToUpperInvariant() ?? "";

    public required List<BusinessPartnerRole> Roles { get; set; } = [];

    public Address? Address { get; set; }

    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>
    /// VAT ID / TIN. Carried onto forwarded charges when this partner is the billing recipient.
    /// <para>
    /// Also the module's strongest duplicate signal for a partner, in the role an IMO number plays
    /// for a vessel: the bureau issues one per legal entity, so two records carrying the same one
    /// are the same company however differently somebody spelled its trading name. Stored
    /// digits-only through <see cref="NormalizeTaxId"/>, because "123-456-789-000" and
    /// "123456789000" are the same TIN and only one of them matches.
    /// </para>
    /// </summary>
    public string? TaxId
    {
        get => taxId;
        set => taxId = NormalizeTaxId(value);
    }

    private string? taxId;

    /// <summary>
    /// Strips the separators a TIN is printed with, leaving the digits. Returns null for a blank
    /// value so the column stays genuinely empty rather than holding an empty string the exact-match
    /// probe would treat as a value every partner without a TIN shares.
    /// </summary>
    public static string? NormalizeTaxId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string([.. value.Where(char.IsAsciiDigit)]);
        return digits.Length == 0 ? null : digits;
    }

    /// <summary>
    /// Set when this partner is also a billable customer, so a file billed to them resolves to the
    /// same accounting record.
    /// </summary>
    public Guid? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch trades with this partner. Scoping governs
    /// stewardship, not visibility - a shipping line one office maintains is still readable
    /// everywhere.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the partner is offered for new work.
    /// <para>
    /// ⚠️ Set by approval, never by a create or an edit. A partner is active exactly when a second
    /// person has accepted it, which is what stops maker-checker being decoration.
    /// </para>
    /// </summary>
    public bool IsActive { get; set; }

    /// <inheritdoc/>
    public MasterDataApprovalState ApprovalState { get; set; } = MasterDataApprovalState.Draft;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Whether this partner may be attached to a workfile slot expecting <paramref name="role"/>.
    /// <para>
    /// Inactive partners never qualify - which now also excludes drafts and anything still sitting
    /// in an approver's queue, since neither is active. Deleted and merged records are excluded
    /// explicitly rather than left to the global query filter: this is called on an entity somebody
    /// else loaded, and a caller that read past the filter must not be able to attach a tombstone
    /// to a live file.
    /// </para>
    /// </summary>
    public bool CanActAs(BusinessPartnerRole role) =>
        IsActive && DeletedAt is null && MergedIntoId is null && Roles.Contains(role);
}
