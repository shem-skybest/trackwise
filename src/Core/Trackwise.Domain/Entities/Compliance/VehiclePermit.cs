using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One paper held by a truck or a trailer - an LTO registration, an LTFRB franchise, an insurance
/// policy - for one validity period. A renewal is a new record, so the history of past papers stays;
/// the one that counts is the latest-expiring record of its type for that vehicle.
/// <para>
/// Belongs to exactly one vehicle: a truck or a trailer, never both. The pair of nullable keys is
/// guarded by a check constraint as well as <see cref="ForTruck" /> / <see cref="ForTrailer" />.
/// </para>
/// </summary>
public class VehiclePermit : AuditableEntity, ITenantEntity
{
    public Guid? TruckId { get; private set; }
    public virtual Truck? Truck { get; private set; }

    public Guid? TrailerId { get; private set; }
    public virtual Trailer? Trailer { get; private set; }

    public VehiclePermitType Type { get; set; }

    private string? number;
    private string? issuer;
    private string? notes;

    /// <summary>The paper's own number: OR/CR number, LTFRB case number, policy number.</summary>
    public string? Number
    {
        get => number;
        set => number = Clean(value);
    }

    /// <summary>Who issued it - an LTO district office, an insurer.</summary>
    public string? Issuer
    {
        get => issuer;
        set => issuer = Clean(value);
    }

    public DateOnly? IssuedOn { get; set; }

    public DateOnly ExpiresOn { get; private set; }

    /// <summary>What this paper cost, in the tenant's currency.</summary>
    public decimal? Cost { get; set; }

    public string? Notes
    {
        get => notes;
        set => notes = Clean(value);
    }

    /// <summary>The threshold (60, 30, 7 days; 0 once expired) of the last reminder sent, so each fires once.</summary>
    public int? LastReminderThresholdDays { get; private set; }

    public DateTime? LastReminderSentAt { get; private set; }

    public static VehiclePermit ForTruck(Guid truckId, VehiclePermitType type, DateOnly expiresOn) =>
        new() { TruckId = truckId, Type = type, ExpiresOn = expiresOn };

    public static VehiclePermit ForTrailer(Guid trailerId, VehiclePermitType type, DateOnly expiresOn) =>
        new() { TrailerId = trailerId, Type = type, ExpiresOn = expiresOn };

    /// <summary>A new expiry date re-arms the reminders: an extended paper must warn again before its new date.</summary>
    public void SetExpiry(DateOnly expiresOn)
    {
        if (expiresOn == ExpiresOn)
        {
            return;
        }

        ExpiresOn = expiresOn;
        LastReminderThresholdDays = null;
        LastReminderSentAt = null;
    }

    public void MarkReminded(int thresholdDays, DateTime sentAt)
    {
        LastReminderThresholdDays = thresholdDays;
        LastReminderSentAt = sentAt;
    }

    public int DaysUntilExpiry(DateOnly today) => ExpiresOn.DayNumber - today.DayNumber;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
