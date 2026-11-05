using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class CorrectTripGarageTimesValidator : AbstractValidator<CorrectTripGarageTimesCommand>
{
    public CorrectTripGarageTimesValidator()
    {
        RuleFor(i => i.Reason).NotEmpty().MaximumLength(500)
            .WithMessage("Say why the recorded value is being corrected (up to 500 characters).");
        RuleFor(i => i.DepartedAt)
            .Must(at => at!.Value.Kind != DateTimeKind.Unspecified)
            .When(i => i.DepartedAt.HasValue)
            .WithMessage("Garage departure time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
        RuleFor(i => i.ArrivedAt)
            .Must(at => at!.Value.Kind != DateTimeKind.Unspecified)
            .When(i => i.ArrivedAt.HasValue)
            .WithMessage("Garage arrival time must include a timezone (e.g. an ISO 8601 'Z' or offset).");
    }
}
