using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Queries;

public class GetDataExportRequestQuery : IQuery<Result<DataExportRequestDto>>
{
    public Guid Id { get; set; }
}
