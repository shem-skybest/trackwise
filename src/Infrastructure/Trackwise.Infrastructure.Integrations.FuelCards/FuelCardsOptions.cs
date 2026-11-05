using Trackwise.Infrastructure.Integrations.FuelCards.Providers.Efs;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers.Wex;

namespace Trackwise.Infrastructure.Integrations.FuelCards;

/// <summary>
/// Aggregate options for the fuel card integrations, mirroring LoadBoardOptions and EldOptions:
/// one section bound once, with a nullable entry per provider.
/// </summary>
public record FuelCardsOptions
{
    public const string SectionName = "FuelCards";
    public WexOptions? Wex { get; set; }
    public EfsOptions? Efs { get; set; }
}
