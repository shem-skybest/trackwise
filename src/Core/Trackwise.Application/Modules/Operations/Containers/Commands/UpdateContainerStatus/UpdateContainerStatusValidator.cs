using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

internal sealed class UpdateContainerStatusValidator : AbstractValidator<UpdateContainerStatusCommand>
{
    public UpdateContainerStatusValidator()
    {
        RuleFor(i => i.TargetStatus).IsInEnum();
        When(i => i.TargetStatus is ContainerStatus.AtPort or ContainerStatus.Returned, () =>
        {
            RuleFor(i => i.TerminalId)
                .NotNull()
                .WithMessage("TerminalId is required for AtPort and Returned transitions.");
        });
    }
}
