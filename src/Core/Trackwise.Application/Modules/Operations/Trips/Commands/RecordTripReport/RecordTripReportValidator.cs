using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripReportValidator : AbstractValidator<RecordTripReportCommand>
{
    private const string NeedsTimezone = "Every time must include a timezone (e.g. an ISO 8601 'Z' or offset).";

    public RecordTripReportValidator()
    {
        RuleFor(i => i.TripId).NotEmpty();
        RuleFor(i => i.GarageDepartedAt).Must(HasTimezone).WithMessage(NeedsTimezone);
        RuleFor(i => i.GarageArrivedAt).Must(HasTimezone).WithMessage(NeedsTimezone);

        RuleFor(i => i.Stops)
            .Must(stops => stops.Select(s => s.StopId).Distinct().Count() == stops.Count)
            .WithMessage("Each stop can appear only once in a report.");

        RuleForEach(i => i.Stops).ChildRules(stop =>
        {
            stop.RuleFor(s => s.StopId).NotEmpty();
            stop.RuleFor(s => s.ArrivedAt).Must(HasTimezone).WithMessage(NeedsTimezone);
            stop.RuleFor(s => s.HandlingStartedAt).Must(HasTimezone).WithMessage(NeedsTimezone);
            stop.RuleFor(s => s.HandlingFinishedAt).Must(HasTimezone).WithMessage(NeedsTimezone);
            stop.RuleFor(s => s.DepartedAt).Must(HasTimezone).WithMessage(NeedsTimezone);
            stop.RuleFor(s => s.Outcome).IsInEnum().When(s => s.Outcome.HasValue);
            stop.RuleFor(s => s.ReturnReason).IsInEnum().When(s => s.ReturnReason.HasValue);
            stop.RuleFor(s => s.ReturnedBoxes).GreaterThanOrEqualTo(0).When(s => s.ReturnedBoxes.HasValue);
            stop.RuleFor(s => s.ReturnedBags).GreaterThanOrEqualTo(0).When(s => s.ReturnedBags.HasValue);
            stop.RuleFor(s => s.OutcomeNote).MaximumLength(500);
            stop.RuleFor(s => s.OutcomeNote)
                .NotEmpty()
                .When(s => s.Outcome is LoadStatus.FoulTrip or LoadStatus.CancelledByClient)
                .WithMessage("Say what happened - a foul trip or client cancellation needs a reason.");
            stop.RuleFor(s => s.DepartedAt)
                .NotNull()
                .When(s => s.HandlingStartedAt.HasValue || s.HandlingFinishedAt.HasValue)
                .WithMessage("Loading and unloading times are saved with the departure - enter it too.");
        });
    }

    private static bool HasTimezone(DateTime? at) => at is not { Kind: DateTimeKind.Unspecified };
}
