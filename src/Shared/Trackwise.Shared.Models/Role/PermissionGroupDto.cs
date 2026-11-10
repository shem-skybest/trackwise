namespace Trackwise.Shared.Models;

/// <summary>One module's permissions, labelled for the role editor (e.g. Bookings: View, Manage, View costs).</summary>
public record PermissionGroupDto
{
    public required string Module { get; init; }
    public required string Label { get; init; }

    /// <summary>The editor's section: Operations, Forwarding, Fleet, Money, People &amp; system, US-only.</summary>
    public required string Section { get; init; }

    public required PermissionOptionDto[] Permissions { get; init; }
}
