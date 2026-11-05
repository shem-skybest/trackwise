using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Queries;

internal sealed class GetUserByIdValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdValidator()
    {
        RuleFor(i => i.UserId)
            .NotEmpty();
    }
}
