using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A client's contracted truck-trip rates for a period: what Cold Chain keeps on each client's
/// Monday quotation board (HFI QUOTATION, PFI QUOTATION...).
/// <para>
/// Every rate is per truck-trip, looked up by where the truck goes (a town or province, or a
/// distance band) and the truck's size, with fuel added on top by <see cref="FuelMethod"/>.
/// A price change is a <b>new agreement</b> (<see cref="CreateRevision"/>), never an edit, so a trip
/// keeps the rates it was priced on; that is what Monday loses today by typing over its rows.
/// </para>
/// <para>
/// Kept apart from <see cref="Tariff"/>: a tariff is a forwarding lane × container rate sheet with
/// no client, truck size or diesel price, and bending it to fit would give both half a set of
/// fields that mean nothing to them.
/// </para>
/// </summary>
public partial class RateAgreement : AuditableEntity, ITenantEntity
{
    public Guid CustomerId { get; private set; }
    public virtual Customer? Customer { get; private set; }

    public string Name { get; private set; } = default!;

    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>Last day in force, or null while it runs until replaced.</summary>
    public DateOnly? EffectiveTo { get; private set; }

    public RateAgreementStatus Status { get; private set; } = RateAgreementStatus.Draft;

    public string Currency { get; private set; } = "PHP";
    public string? Notes { get; private set; }

    /// <summary>The agreement this one revises, if any.</summary>
    public Guid? SupersedesId { get; private set; }

    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    public virtual List<RateAgreementLine> Lines { get; private set; } = [];

    public static RateAgreement Create(
        Guid customerId,
        string name,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        RateAgreementTerms terms,
        string? notes)
    {
        var agreement = new RateAgreement { CustomerId = customerId };
        agreement.SetHeader(name, effectiveFrom, effectiveTo, notes);
        agreement.SetTerms(terms);
        return agreement;
    }

    /// <summary>
    /// Changes the header of a draft. An approved agreement is changed by revising it. Returns the
    /// extra-drop charges to add: the caller deletes the old ones and registers these with the
    /// repository, as <see cref="ReplaceLines"/> does for lines.
    /// </summary>
    public IReadOnlyList<RateExtraDropCharge> UpdateTerms(
        string name,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        RateAgreementTerms terms,
        string? notes)
    {
        EnsureDraft();
        SetHeader(name, effectiveFrom, effectiveTo, notes);
        SetTerms(terms);
        return ExtraDropCharges;
    }

    /// <summary>
    /// Replaces the rate lines of a draft. Returns the lines to add so the caller can register them
    /// with the repository - a line reaching the tracker only through this collection would be saved
    /// as an update to a row that does not exist.
    /// </summary>
    public IReadOnlyList<RateAgreementLine> ReplaceLines(IEnumerable<RateLineSpec> specs)
    {
        EnsureDraft();
        Lines.Clear();

        var order = 0;
        foreach (var spec in specs)
        {
            Lines.Add(RateAgreementLine.Create(Id, order++, spec));
        }

        return Lines;
    }

    /// <summary>Puts a draft in force. The caller closes whatever agreement it replaces.</summary>
    public void Approve(string approvedBy, DateTime approvedAt)
    {
        EnsureDraft();

        if (Lines.Count == 0)
        {
            throw new InvalidOperationException("An agreement needs at least one rate line before it can be approved.");
        }

        Status = RateAgreementStatus.Active;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    /// <summary>Closes an active agreement the day before its replacement starts.</summary>
    public void SupersedeFrom(DateOnly replacementStartsOn)
    {
        if (Status is not RateAgreementStatus.Active)
        {
            throw new InvalidOperationException("Only an active agreement can be superseded.");
        }

        if (replacementStartsOn <= EffectiveFrom)
        {
            throw new InvalidOperationException(
                $"'{Name}' starts {EffectiveFrom:yyyy-MM-dd}; a replacement must start after it.");
        }

        Status = RateAgreementStatus.Superseded;
        EffectiveTo = replacementStartsOn.AddDays(-1);
    }

    public void Withdraw()
    {
        if (Status is RateAgreementStatus.Superseded or RateAgreementStatus.Withdrawn)
        {
            throw new InvalidOperationException($"This agreement is already {Status.ToString().ToLowerInvariant()}.");
        }

        Status = RateAgreementStatus.Withdrawn;
    }

    /// <summary>
    /// A new draft carrying these terms and lines, starting <paramref name="effectiveFrom"/>. This one
    /// stays in force until the revision is approved.
    /// </summary>
    public RateAgreement CreateRevision(string name, DateOnly effectiveFrom)
    {
        if (Status is not (RateAgreementStatus.Active or RateAgreementStatus.Superseded))
        {
            throw new InvalidOperationException("Only an approved agreement can be revised; edit the draft instead.");
        }

        var revision = Create(CustomerId, name, effectiveFrom, null, Terms, Notes);
        revision.SupersedesId = Id;
        revision.ReplaceLines(Lines.OrderBy(l => l.SortOrder).Select(l => l.ToSpec()));
        return revision;
    }

    public bool IsInForceOn(DateOnly date) =>
        Status is RateAgreementStatus.Active or RateAgreementStatus.Superseded
        && EffectiveFrom <= date
        && (EffectiveTo is null || date <= EffectiveTo);

    private void SetHeader(string name, DateOnly effectiveFrom, DateOnly? effectiveTo, string? notes)
    {
        if (effectiveTo is { } to && to < effectiveFrom)
        {
            throw new InvalidOperationException("The agreement can't end before it starts.");
        }

        Name = name.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    private void EnsureDraft()
    {
        if (Status is not RateAgreementStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft can be changed. Revise the agreement to change its rates.");
        }
    }
}
