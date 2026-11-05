using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Loads;

/// <summary>
/// Which saved sites a load may point at. Shared by create and update so the two cannot drift.
/// </summary>
internal static class LoadSiteRules
{
    /// <summary>
    /// Why <paramref name="site"/> cannot be a pickup or drop for a load of
    /// <paramref name="customerId"/>, or null when it can.
    /// </summary>
    /// <param name="isNewLink">
    /// False when the load already pointed here. A site deactivated since keeps the loads it has;
    /// only new work is refused.
    /// </param>
    public static string? Refusal(Location site, Guid customerId, bool isNewLink)
    {
        if (site.Type is LocationType.Zone)
        {
            return $"'{site.Name}' is a zone, not somewhere a truck goes. Pick a site inside it.";
        }

        if (site.CustomerId is { } owner && owner != customerId)
        {
            return $"'{site.Name}' belongs to another customer. Pick one of this customer's sites or a shared one.";
        }

        if (isNewLink && !site.IsActive)
        {
            return $"'{site.Name}' is no longer active for new work.";
        }

        return null;
    }
}
