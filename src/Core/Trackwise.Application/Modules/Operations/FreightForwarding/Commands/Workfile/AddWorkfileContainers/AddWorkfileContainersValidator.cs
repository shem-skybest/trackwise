using FluentValidation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Containers;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class AddWorkfileContainersValidator : WorkfileContainerFieldsValidator<AddWorkfileContainersCommand>
{
    public AddWorkfileContainersValidator()
    {
        RuleFor(i => i.Quantity).InclusiveBetween(1, 50);
        RuleFor(i => i.Quantity)
            .Equal(1)
            .When(i => !string.IsNullOrWhiteSpace(i.ContainerNumber))
            .WithMessage("A container number names one box - add numbered boxes one at a time.");
    }
}
