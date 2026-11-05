using FluentValidation;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class CreateSetupIntentValidator : AbstractValidator<CreateSetupIntentCommand>
{
    public CreateSetupIntentValidator()
    {

    }
}
