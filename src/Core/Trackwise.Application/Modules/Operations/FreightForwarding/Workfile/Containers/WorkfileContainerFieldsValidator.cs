using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Containers;

/// <summary>The rules adding and editing a container slot share.</summary>
internal abstract class WorkfileContainerFieldsValidator<T> : AbstractValidator<T> where T : WorkfileContainerFields
{
    protected WorkfileContainerFieldsValidator()
    {
        RuleFor(i => i.ContainerTypeId).NotEmpty().WithMessage("Pick a container size.");
        RuleFor(i => i.ContainerNumber)
            .Matches("^[A-Za-z]{4}[0-9]{7}$")
            .When(i => !string.IsNullOrWhiteSpace(i.ContainerNumber))
            .WithMessage("A container number is 4 letters and 7 digits, like MAEU7025097.");
        RuleFor(i => i.SealNumber).MaximumLength(50);
        RuleFor(i => i.GrossWeightKg).GreaterThanOrEqualTo(0).When(i => i.GrossWeightKg.HasValue);
        RuleFor(i => i.PackageCount).GreaterThanOrEqualTo(0).When(i => i.PackageCount.HasValue);
        RuleFor(i => i.PackagingType).MaximumLength(50);
        RuleFor(i => i.Cbm).GreaterThanOrEqualTo(0).When(i => i.Cbm.HasValue);
        RuleFor(i => i.Notes).MaximumLength(500);
    }
}
