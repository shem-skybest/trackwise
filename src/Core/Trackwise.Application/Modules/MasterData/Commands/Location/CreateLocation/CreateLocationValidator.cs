using FluentValidation;
using Trackwise.Application.Validators;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateLocationValidator : AbstractValidator<CreateLocationCommand>
{
    public CreateLocationValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Required everywhere except a zone, which is a grouping rather than a place. Splitting the
        // rule in two is what lets the message say which case the steward is in.
        RuleFor(i => i.Address)
            .NotNull()
            .WithMessage("A location needs an address. Only a zone can be left without one.")
            .SetValidator(new AddressValidator()!)
            .When(i => i.Type is not LocationType.Zone);

        RuleFor(i => i.Address)
            .Null()
            .WithMessage("A zone groups other locations and has no address of its own.")
            .When(i => i.Type is LocationType.Zone);

        // Coordinates travel together. One half of a pair points at the Gulf of Guinea, which is
        // where every half-filled coordinate on a map ends up.
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

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing locations.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
