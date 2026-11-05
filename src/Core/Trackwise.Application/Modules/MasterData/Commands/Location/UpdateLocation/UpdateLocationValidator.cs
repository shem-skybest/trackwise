using FluentValidation;
using Trackwise.Application.Validators;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateLocationValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.Notes).MaximumLength(2000);

        RuleFor(i => i.Address)
            .NotNull()
            .WithMessage("A location needs an address. Only a zone can be left without one.")
            .SetValidator(new AddressValidator()!)
            .When(i => i.Type is not LocationType.Zone);

        RuleFor(i => i.Address)
            .Null()
            .WithMessage("A zone groups other locations and has no address of its own.")
            .When(i => i.Type is LocationType.Zone);

        RuleFor(i => i.Latitude)
            .NotNull()
            .WithMessage("Give both latitude and longitude, or neither.")
            .When(i => i.Longitude.HasValue);

        RuleFor(i => i.Longitude)
            .NotNull()
            .WithMessage("Give both latitude and longitude, or neither.")
            .When(i => i.Latitude.HasValue);

        RuleFor(i => i.Latitude).InclusiveBetween(-90, 90).When(i => i.Latitude.HasValue);

        // Under 10 m is inside GPS error, so nothing would ever arrive; over 5 km is a district,
        // not a site.
        RuleFor(i => i.ZoneRadiusMeters).InclusiveBetween(10, 5000).When(i => i.ZoneRadiusMeters.HasValue);
        RuleFor(i => i.Longitude).InclusiveBetween(-180, 180).When(i => i.Longitude.HasValue);

        // A location cannot be its own zone. The deeper loops need a database walk and live in
        // LocationAncestry; this catches the one case that needs no query at all.
        RuleFor(i => i.ParentLocationId)
            .NotEqual(i => i.Id)
            .WithMessage("A location cannot sit inside itself.")
            .When(i => i.ParentLocationId.HasValue);

        // Retiring a location things still depend on is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a location that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
