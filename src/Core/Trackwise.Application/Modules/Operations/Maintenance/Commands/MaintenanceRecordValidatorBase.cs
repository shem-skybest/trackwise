using FluentValidation;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal abstract class MaintenanceRecordValidatorBase<T> : AbstractValidator<T> where T : IMaintenanceRecordFields
{
    protected MaintenanceRecordValidatorBase()
    {
        RuleFor(i => i.TruckId).NotEmpty();
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.VehicleSystem).IsInEnum();
        RuleFor(i => i.Description).NotEmpty().MaximumLength(1000);
        RuleFor(i => i.ServiceDate)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("The service date can't be in the future.");
        RuleFor(i => i.OdometerReading).GreaterThanOrEqualTo(0);
        RuleFor(i => i.EngineHours).GreaterThanOrEqualTo(0);
        RuleFor(i => i.RequisitionNumber).MaximumLength(50);
        RuleFor(i => i.VendorName).MaximumLength(200);
        RuleFor(i => i.InvoiceNumber).MaximumLength(100);
        RuleFor(i => i.LaborCost).GreaterThanOrEqualTo(0);
        RuleFor(i => i.PartsCost).GreaterThanOrEqualTo(0);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleForEach(i => i.Parts).ChildRules(part =>
        {
            part.RuleFor(p => p.PartName).NotEmpty().MaximumLength(200);
            part.RuleFor(p => p.Description).MaximumLength(300);
            part.RuleFor(p => p.PartNumber).MaximumLength(100);
            part.RuleFor(p => p.Quantity).GreaterThan(0);
            part.RuleFor(p => p.UnitCost).GreaterThanOrEqualTo(0);
        });
    }
}
