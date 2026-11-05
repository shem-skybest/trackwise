using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Loads.Queries;

public class GetLoadExceptionsQuery : IQuery<Result<IEnumerable<LoadExceptionDto>>>
{
    public Guid LoadId { get; set; }
}
