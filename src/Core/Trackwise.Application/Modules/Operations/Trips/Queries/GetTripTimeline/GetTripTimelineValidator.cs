using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripTimelineValidator : AbstractValidator<GetTripTimelineQuery>
{
    public GetTripTimelineValidator()
    {
        RuleFor(i => i.TripId).NotEmpty();
    }
}
