using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Rewrites a draft's terms and its whole rate card. An approved agreement is changed by revising
/// it, so trips already priced keep the rates they were priced on.
/// </summary>
public class UpdateRateAgreementCommand : RateAgreementTermsCommand, ICommand<Result<RateAgreementDto>>, IHaveId
{
    public Guid Id { get; set; }
}
