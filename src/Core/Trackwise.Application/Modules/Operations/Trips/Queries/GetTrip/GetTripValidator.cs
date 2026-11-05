using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripValidator : AbstractValidator<GetTripQuery>
{
    public GetTripValidator()
    {
        RuleFor(i => i.TripId).NotEmpty();
    }
}
