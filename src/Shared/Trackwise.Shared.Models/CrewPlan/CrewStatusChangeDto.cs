namespace Trackwise.Shared.Models;

/// <summary>What setting a crew member's status changed.</summary>
public record CrewStatusChangeDto
{
    /// <summary>How many days were set.</summary>
    public int Days { get; set; }

    /// <summary>
    /// Trucks the person was taken off because the status keeps them from working, as
    /// "ABC1267 on 2026-09-30". Empty otherwise.
    /// </summary>
    public IList<string> RemovedFrom { get; set; } = [];
}
