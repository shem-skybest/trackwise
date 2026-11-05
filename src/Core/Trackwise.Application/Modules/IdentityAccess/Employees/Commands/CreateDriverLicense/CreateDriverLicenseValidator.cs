using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

internal sealed class CreateDriverLicenseValidator : AbstractValidator<CreateDriverLicenseCommand>
{
    public CreateDriverLicenseValidator()
    {
        RuleFor(i => i.EmployeeId).NotEmpty();
        RuleFor(i => i.LicenseNumber).NotEmpty().MaximumLength(64);

        RuleFor(i => i.IssuingCountry)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Z]{2}$")
            .WithMessage("Issuing country must be an ISO 3166-1 alpha-2 code (e.g., 'US', 'DE').");

        RuleFor(i => i.IssuingRegion).MaximumLength(64);
        RuleFor(i => i.IssuedDate).NotEmpty();
        RuleFor(i => i.ExpiresAt)
            .NotEmpty()
            .GreaterThan(i => i.IssuedDate)
            .WithMessage("Expiry must be after issued date.");

        RuleFor(i => i.LicenseClass)
            .Must((cmd, cls) => cls.IsValidForCountry(cmd.IssuingCountry))
            .WithMessage("License class is not valid for the issuing country.");

        RuleFor(i => i.DlCodes)
            .NotEmpty()
            .When(i => i.LicenseClass.IsPhilippine())
            .WithMessage("A Philippine license needs at least one DL code (e.g. B2, C, CE).");

        RuleFor(i => i.DlCodes)
            .Empty()
            .When(i => !i.LicenseClass.IsPhilippine())
            .WithMessage("DL codes only apply to Philippine licenses.");
    }
}
