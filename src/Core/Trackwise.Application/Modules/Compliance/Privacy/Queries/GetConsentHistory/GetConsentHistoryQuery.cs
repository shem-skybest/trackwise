using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

public class GetConsentHistoryQuery : IQuery<Result<List<ConsentRecordDto>>>
{
}
