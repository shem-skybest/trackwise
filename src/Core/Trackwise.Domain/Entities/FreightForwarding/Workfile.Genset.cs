using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
// The property below shares its name with the enum type (matches CreditType's existing pattern),
// which shadows the type name in expression position. This alias is how the enum's members get
// referenced from inside this partial without qualifying the whole namespace every time.
using GensetBillingKind = Trackwise.Domain.Primitives.Enums.GensetBillingType;

namespace Trackwise.Domain.Entities;

public abstract partial class Workfile
{
    /// <summary>
    /// Attaches or replaces the genset add-on. Genset has never been seen booked standalone - it is
    /// always attached to an existing (reefer) file, never part of creation - so this is its own
    /// verb rather than a header field.
    /// <para>
    /// <paramref name="gensetId"/> is optional: ops may record that a genset was requested and how
    /// it is billed before a specific physical unit is assigned - the source data shows plate/unit
    /// assignment routinely lagging the booking itself.
    /// </para>
    /// </summary>
    public void SetGensetAddOn(
        Guid? gensetId,
        GensetBillingType billingType,
        decimal? hourlyRate,
        decimal? flatAmount,
        string? notes)
    {
        EnsureGensetSupported();
        EnsureEditable();

        if (billingType == GensetBillingKind.Itemized && hourlyRate is not > 0)
        {
            throw new InvalidOperationException(
                "An itemized genset add-on requires a positive hourly rate.");
        }

        if (billingType == GensetBillingKind.ProfitShare && flatAmount is not > 0)
        {
            throw new InvalidOperationException(
                "A profit-share genset add-on requires a positive flat amount.");
        }

        // Switching billing type discards hours already logged against the old shape - a
        // profit-share job was never tracking hours to begin with, and hours logged before an
        // itemized rate existed have no rate to price against. A same-type edit (e.g. correcting
        // the rate) leaves recorded hours alone.
        if (GensetBillingType != billingType)
        {
            GensetHoursUsed = null;
        }

        GensetId = gensetId;
        GensetBillingType = billingType;
        GensetHourlyRate = billingType == GensetBillingKind.Itemized ? hourlyRate : null;
        GensetFlatAmount = billingType == GensetBillingKind.ProfitShare ? flatAmount : null;
        GensetNotes = notes;

        DomainEvents.Add(new WorkfileGensetAddOnSet(Id, gensetId, billingType));
    }

    /// <summary>
    /// Records actual genset hours as the job runs. Itemized only - a profit-share job's charge is
    /// flat regardless of hours, so there is nothing here to record against.
    /// </summary>
    public void RecordGensetHoursUsed(decimal hoursUsed)
    {
        EnsureGensetSupported();
        EnsureEditable();

        if (GensetBillingType is not GensetBillingKind.Itemized)
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' has no itemized genset add-on to record hours against.");
        }

        if (hoursUsed < 0)
        {
            throw new InvalidOperationException("Genset hours used cannot be negative.");
        }

        GensetHoursUsed = hoursUsed;
        DomainEvents.Add(new WorkfileGensetHoursRecorded(Id, hoursUsed));
    }

    public void RemoveGensetAddOn()
    {
        EnsureGensetSupported();
        EnsureEditable();

        if (GensetBillingType is null)
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' has no genset add-on to remove.");
        }

        GensetId = null;
        GensetBillingType = null;
        GensetHourlyRate = null;
        GensetHoursUsed = null;
        GensetFlatAmount = null;
        GensetNotes = null;

        DomainEvents.Add(new WorkfileGensetAddOnRemoved(Id));
    }

    private void EnsureGensetSupported()
    {
        if (!SupportsGenset)
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' is a {Type} file and does not support a genset add-on.");
        }
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException(
                $"Workfile '{ReferenceNumber}' cannot be edited in its current state.");
        }
    }
}

public record WorkfileGensetAddOnSet(Guid Id, Guid? GensetId, GensetBillingType BillingType) : IDomainEvent;
public record WorkfileGensetHoursRecorded(Guid Id, decimal HoursUsed) : IDomainEvent;
public record WorkfileGensetAddOnRemoved(Guid Id) : IDomainEvent;
