using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Puts a draft in force. The client's agreement running when it starts is closed the day before,
/// so exactly one agreement prices any given trip date.
/// </summary>
public class ApproveRateAgreementCommand : ICommand<Result<RateAgreementDto>>, IHaveId
{
    public Guid Id { get; set; }
}
