using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetMasterDataApprovalsValidator : AbstractValidator<GetMasterDataApprovalsQuery>
{
    public GetMasterDataApprovalsValidator()
    {
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 500);
        RuleFor(i => i.EntityType).MaximumLength(128);
    }
}
