namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Outcome of the most recent push of a local entity to the accounting provider.
/// </summary>
public enum QboSyncStatus
{
    Synced = 1,
    Failed = 2
}
