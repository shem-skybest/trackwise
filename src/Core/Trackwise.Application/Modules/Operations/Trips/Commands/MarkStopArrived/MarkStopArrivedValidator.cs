using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class MarkStopArrivedValidator : AbstractValidator<MarkStopArrivedCommand>
{
    public MarkStopArrivedValidator()
    {
        RuleFor(i => i.TripId).NotEmpty();
        RuleFor(i => i.StopId).NotEmpty();
        RuleFor(i => i.ArrivedAt)
            .Must(at => at!.Value.Kind != DateTimeKind.Unspecified)
            .When(i => i.ArrivedAt.HasValue)
            .WithMessage("Arrival time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
    }
}
