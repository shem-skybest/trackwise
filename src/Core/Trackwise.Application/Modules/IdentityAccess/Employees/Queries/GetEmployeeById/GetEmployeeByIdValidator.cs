using FluentValidation;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

internal sealed class GetEmployeeByIdValidator : AbstractValidator<GetEmployeeByIdQuery>
{
    public GetEmployeeByIdValidator()
    {
        RuleFor(i => i.UserId).NotEmpty();
    }
}
