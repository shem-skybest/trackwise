using FluentValidation;

namespace Trackwise.Application.Modules.Integrations.Webhooks.Commands;

internal sealed class ProcessStripEventValidator : AbstractValidator<ProcessStripEventCommand>
{
    public ProcessStripEventValidator()
    {
        RuleFor(i => i.RequestBodyJson)
            .NotNull();

        RuleFor(i => i.StripeSignature)
            .NotNull();
    }
}
