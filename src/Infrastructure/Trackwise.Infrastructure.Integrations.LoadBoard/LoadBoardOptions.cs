using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Dat;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.OneTwo3;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;

namespace Trackwise.Infrastructure.Integrations.LoadBoard;

public record LoadBoardOptions
{
    public const string SectionName = "LoadBoard";
    public DatOptions? Dat { get; set; }
    public TruckstopOptions? Truckstop { get; set; }
    public OneTwo3LoadboardOptions? OneTwo3Loadboard { get; set; }
}
