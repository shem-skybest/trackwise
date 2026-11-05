using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetPartnerByIdQuery : IQuery<Result<BusinessPartnerDto>>, IHaveId
{
    public Guid Id { get; set; }
}
