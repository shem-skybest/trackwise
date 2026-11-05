using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetWorkfilesValidator : AbstractValidator<GetWorkfilesQuery>
{
    public GetWorkfilesValidator()
    {
        RuleFor(i => i.Page).GreaterThan(0);
        RuleFor(i => i.PageSize).GreaterThan(0);
    }
}
