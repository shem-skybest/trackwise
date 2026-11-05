using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal abstract class MaintenanceScheduleValidatorBase<T> : AbstractValidator<T> where T : IMaintenanceScheduleFields
{
    protected MaintenanceScheduleValidatorBase()
    {
        RuleFor(i => i.IntervalType).IsInEnum();
        RuleFor(i => i.MileageInterval).GreaterThan(0);
        RuleFor(i => i.DaysInterval).GreaterThan(0);
        RuleFor(i => i.EngineHoursInterval).GreaterThan(0);
        RuleFor(i => i.LastServiceDate)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("The last service date can't be in the future.");
        RuleFor(i => i.LastServiceOdometer).GreaterThanOrEqualTo(0);
        RuleFor(i => i.LastServiceEngineHours).GreaterThanOrEqualTo(0);
        RuleFor(i => i.Notes).MaximumLength(1000);
    }
}
