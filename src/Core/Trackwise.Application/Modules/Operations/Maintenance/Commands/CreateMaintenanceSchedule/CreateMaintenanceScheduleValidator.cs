using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class CreateMaintenanceScheduleValidator : MaintenanceScheduleValidatorBase<CreateMaintenanceScheduleCommand>
{
    public CreateMaintenanceScheduleValidator()
    {
        RuleFor(i => i.TruckId).NotEmpty();
        RuleFor(i => i.Type).IsInEnum();
    }
}
