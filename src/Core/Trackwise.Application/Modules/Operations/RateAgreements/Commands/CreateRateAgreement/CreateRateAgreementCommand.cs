using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>Creates a draft agreement. It prices nothing until approved.</summary>
public class CreateRateAgreementCommand : RateAgreementTermsCommand, ICommand<Result<RateAgreementDto>>
{
    public Guid CustomerId { get; set; }
}
