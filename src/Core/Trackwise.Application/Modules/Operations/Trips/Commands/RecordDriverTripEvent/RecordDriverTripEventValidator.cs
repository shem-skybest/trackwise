using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordDriverTripEventValidator : AbstractValidator<RecordDriverTripEventCommand>
{
    public RecordDriverTripEventValidator()
    {
        RuleFor(i => i.TripId).NotEmpty();
        RuleFor(i => i.StopId)
            .NotEmpty()
            .When(i => i.Kind is not (TripEventKind.GarageDeparted or TripEventKind.GarageArrived))
            .WithMessage("Say which stop this is for.");
        RuleFor(i => i.At)
            .Must(at => at!.Value.Kind != DateTimeKind.Unspecified)
            .When(i => i.At.HasValue)
            .WithMessage("The time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
        RuleFor(i => i.OutcomeNote).MaximumLength(500);
    }
}
