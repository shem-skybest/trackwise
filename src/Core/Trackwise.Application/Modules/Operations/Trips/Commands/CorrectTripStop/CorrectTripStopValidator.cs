using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class CorrectTripStopValidator : AbstractValidator<CorrectTripStopCommand>
{
    public CorrectTripStopValidator()
    {
        RuleFor(i => i.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Say why the recorded value is being corrected (up to 500 characters).");
        RuleFor(i => i.Outcome).IsInEnum().When(i => i.Outcome.HasValue);
        RuleFor(i => i.ReturnReason).IsInEnum().When(i => i.ReturnReason.HasValue);
        RuleFor(i => i.ReturnedBoxes).GreaterThanOrEqualTo(0).When(i => i.ReturnedBoxes.HasValue);
        RuleFor(i => i.ReturnedBags).GreaterThanOrEqualTo(0).When(i => i.ReturnedBags.HasValue);
        RuleFor(i => i.OutcomeNote).MaximumLength(500);

        RuleFor(i => i.OutcomeNote)
            .NotEmpty()
            .When(i => i.Outcome is LoadStatus.FoulTrip or LoadStatus.CancelledByClient)
            .WithMessage("Say what happened - a foul trip or client cancellation needs a reason.");

        RuleFor(i => i.ArrivedAt).Must(at => at.Kind != DateTimeKind.Unspecified)
            .WithMessage("Arrival time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
        RuleFor(i => i.DepartedAt).Must(HasTimezone!).When(i => i.DepartedAt.HasValue)
            .WithMessage("Departure time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
        RuleFor(i => i.HandlingStartedAt).Must(HasTimezone!).When(i => i.HandlingStartedAt.HasValue)
            .WithMessage("Handling start time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
        RuleFor(i => i.HandlingFinishedAt).Must(HasTimezone!).When(i => i.HandlingFinishedAt.HasValue)
            .WithMessage("Handling finish time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
    }

    private static bool HasTimezone(DateTime? at) => at!.Value.Kind != DateTimeKind.Unspecified;
}
