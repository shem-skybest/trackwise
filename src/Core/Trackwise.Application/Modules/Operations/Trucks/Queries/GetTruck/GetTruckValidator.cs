using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trucks.Queries;

internal sealed class GetTruckValidator : AbstractValidator<GetTruckQuery>
{
    public GetTruckValidator()
    {
        RuleFor(i => i.TruckOrDriverId)
            .NotEmpty()
            .WithMessage("Specify either TruckId or driver UserId.");
    }
}
