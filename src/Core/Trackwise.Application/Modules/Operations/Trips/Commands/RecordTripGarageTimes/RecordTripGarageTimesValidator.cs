using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripGarageTimesValidator : AbstractValidator<RecordTripGarageTimesCommand>
{
    public RecordTripGarageTimesValidator()
    {
        RuleFor(i => i)
            .Must(i => i.DepartedAt.HasValue || i.ArrivedAt.HasValue)
            .WithMessage("Enter a garage departure or arrival time.");
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
