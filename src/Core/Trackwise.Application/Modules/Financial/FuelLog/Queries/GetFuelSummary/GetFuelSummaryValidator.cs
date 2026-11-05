using FluentValidation;

namespace Trackwise.Application.Modules.Financial.FuelLog.Queries;

internal sealed class GetFuelSummaryValidator : AbstractValidator<GetFuelSummaryQuery>
{
    public GetFuelSummaryValidator()
    {
        RuleFor(q => q.From).NotEmpty().WithMessage("Pick the first day of the period.");
        RuleFor(q => q.To).NotEmpty().WithMessage("Pick the last day of the period.");
        RuleFor(q => q)
            .Must(q => q.To >= q.From && q.To!.Value.DayNumber - q.From!.Value.DayNumber <= 366)
            .When(q => q.From.HasValue && q.To.HasValue)
            .WithMessage("The period must end after it starts and span at most a year.");
    }
}
