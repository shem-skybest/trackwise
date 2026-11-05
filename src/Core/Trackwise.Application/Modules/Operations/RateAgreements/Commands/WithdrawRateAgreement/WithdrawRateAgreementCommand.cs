using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Calls off a draft or an agreement in force. Trips already priced on it keep their price; new
/// trips find no agreement until another is approved.
/// </summary>
public class WithdrawRateAgreementCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
