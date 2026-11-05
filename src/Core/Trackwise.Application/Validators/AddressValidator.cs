using FluentValidation;

using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Validators;

/// <summary>
/// Reusable validator for the <see cref="Address"/> value object.
/// Province and Region are required across all countries - the fields double as
/// "Province / State / Region" with a country-aware label on the client.
/// </summary>
internal sealed class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(a => a.StreetAddress)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(a => a.Subdivision)
            .MaximumLength(200);

        RuleFor(a => a.Barangay)
            .MaximumLength(100);

        RuleFor(a => a.CityOrMunicipality)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(a => a.Province)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Province / state is required.");

        RuleFor(a => a.Region)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Region is required.");

        RuleFor(a => a.Zipcode)
            .MaximumLength(20);

        // Optional in the Philippines: local delivery addresses are routinely kept without one
        // (the Monday delivery board's 1,900 real outlets carry almost none), and refusing them
        // made every imported outlet unusable on a load. Wrong is still refused.
        RuleFor(a => a.Zipcode)
            .NotEmpty()
            .When(a => a.Country != "PH");

        RuleFor(a => a.Zipcode)
            .Matches(@"^\d{4}$")
            .When(a => a.Country == "PH" && !string.IsNullOrEmpty(a.Zipcode))
            .WithMessage("Philippine ZIP codes must be exactly 4 digits.");

        RuleFor(a => a.Country)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Country name must not exceed 100 characters.");
    }
}
