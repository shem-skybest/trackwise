using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Queries;

public class GetLoadBoardConfigurationsQuery : IQuery<Result<List<LoadBoardConfigurationDto>>>
{
}
