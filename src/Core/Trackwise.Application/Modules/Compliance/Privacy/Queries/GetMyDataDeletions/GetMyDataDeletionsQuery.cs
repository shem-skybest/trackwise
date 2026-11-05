using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

public class GetMyDataDeletionsQuery : IQuery<Result<List<DataDeletionRequestDto>>>
{
}
