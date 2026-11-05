using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

public class GetMyDataExportsQuery : IQuery<Result<List<DataExportRequestDto>>>
{
}
