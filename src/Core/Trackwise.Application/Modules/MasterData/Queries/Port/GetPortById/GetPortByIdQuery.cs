using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetPortByIdQuery : IQuery<Result<TerminalDto>>, IHaveId
{
    public Guid Id { get; set; }
}
