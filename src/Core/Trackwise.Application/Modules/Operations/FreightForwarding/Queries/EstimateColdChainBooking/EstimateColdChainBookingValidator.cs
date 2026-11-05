using FluentValidation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class EstimateColdChainBookingValidator : ColdChainBookingPlanValidator<EstimateColdChainBookingQuery>
{
    public EstimateColdChainBookingValidator()
    {
        RuleFor(i => i.CustomerId).NotEmpty();
    }
}
