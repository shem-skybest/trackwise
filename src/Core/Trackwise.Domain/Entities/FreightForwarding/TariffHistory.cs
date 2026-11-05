using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Tracks version history for a tariff.
/// </summary>
public class TariffHistory : AuditableEntity, ITenantEntity
{
    public Guid TariffId { get; private set; }
    public int OldVersion { get; private set; }
    public int NewVersion { get; private set; }
    public string? ChangedBy { get; private set; }
    public string? ChangeReason { get; private set; }

    public static TariffHistory Create(Guid tariffId, int oldVersion, int newVersion,
        string? changedBy, string? changeReason)
    {
        var h = new TariffHistory
        {
            TariffId = tariffId,
            OldVersion = oldVersion,
            NewVersion = newVersion,
            ChangedBy = changedBy,
            ChangeReason = changeReason
        };
        h.DomainEvents.Add(new TariffHistoryCreated(h.Id, tariffId, oldVersion, newVersion));
        return h;
    }
}