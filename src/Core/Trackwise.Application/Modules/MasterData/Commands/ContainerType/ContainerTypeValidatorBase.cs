using FluentValidation;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// The dimension rules create and edit share. One base rather than two copies, because the two
/// forms describe the same box and a rule that drifted between them would let an edit reach a
/// state a create refuses.
/// </summary>
internal abstract class ContainerTypeValidatorBase<T> : AbstractValidator<T>
    where T : IContainerTypeFields
{
    protected ContainerTypeValidatorBase()
    {
        // Checked on the normalised value: paperwork writes the code as "22 G1", and rejecting that
        // would teach stewards to strip the space by hand. Four characters is the whole standard -
        // two for the size, two for the type.
        RuleFor(x => x.IsoSizeTypeCode)
            .Must(value => ContainerType.NormalizeIsoSizeTypeCode(value) is { Length: 4 })
            .WithMessage("An ISO 6346 size-type code is four characters, e.g. 22G1.")
            .When(x => !string.IsNullOrWhiteSpace(x.IsoSizeTypeCode));

        // The lengths that exist. A free integer would admit a 37' box, and the record would then
        // price and stow as something no chassis carries.
        RuleFor(x => x.LengthFeet)
            .Must(value => value is 10 or 20 or 40 or 45 or 53)
            .WithMessage("Container length is 10, 20, 40, 45 or 53 feet. Leave it blank for equipment that has none.")
            .When(x => x.LengthFeet.HasValue);

        // High cube is a statement about a box's height, so it needs a box. Ticked on a road tanker
        // it would read later as a fact somebody established.
        RuleFor(x => x.IsHighCube)
            .Equal(false)
            .WithMessage("High cube describes a container's height, so it needs a length.")
            .When(x => !x.LengthFeet.HasValue);

        RuleFor(x => x.TeuFactor)
            .InclusiveBetween(0.5m, 4m)
            .WithMessage("A TEU factor is between 0.5 and 4 - a 20' is 1, a 40' is 2, a 45' is 2.25.")
            .When(x => x.TeuFactor.HasValue);
    }
}
