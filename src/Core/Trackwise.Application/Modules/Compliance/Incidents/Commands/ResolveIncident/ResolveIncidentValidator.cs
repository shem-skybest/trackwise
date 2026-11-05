using FluentValidation;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class ResolveIncidentValidator : AbstractValidator<ResolveIncidentCommand>
{
    public ResolveIncidentValidator()
    {
        RuleFor(i => i.CorrectiveAction).NotEmpty().MaximumLength(4000)
            .WithMessage("Say what was done about it (up to 4,000 characters).");
    }
}
