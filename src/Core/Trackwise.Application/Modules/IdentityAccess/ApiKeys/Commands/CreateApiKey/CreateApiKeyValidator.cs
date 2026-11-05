using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.ApiKeys.Commands;

internal sealed class CreateApiKeyValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
