using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class RepeatColdChainBookingValidator : AbstractValidator<RepeatColdChainBookingCommand>
{
    public const int MaxDates = 62;

    public RepeatColdChainBookingValidator()
    {
        RuleFor(i => i.Dates).NotEmpty().WithMessage("Pick at least one day.");
        RuleFor(i => i.Dates)
            .Must(dates => dates.Count <= MaxDates)
            .WithMessage($"Repeat for at most {MaxDates} days at a time.");
        RuleFor(i => i.Dates)
            .Must(dates => dates.Distinct().Count() == dates.Count)
            .WithMessage("Each day can only be picked once.");
    }
}
