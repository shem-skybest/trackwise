using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Eld.Queries;

public class GetEldProviderConfigurationsQuery : IQuery<Result<List<EldProviderConfigurationDto>>>
{
}
