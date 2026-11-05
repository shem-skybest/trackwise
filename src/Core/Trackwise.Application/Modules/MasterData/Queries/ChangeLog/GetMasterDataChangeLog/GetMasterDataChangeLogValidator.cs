using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetMasterDataChangeLogValidator : AbstractValidator<GetMasterDataChangeLogQuery>
{
    public GetMasterDataChangeLogValidator()
    {
        RuleFor(i => i.EntityType).NotEmpty().MaximumLength(128);
        RuleFor(i => i.EntityId).NotEmpty();
        RuleFor(i => i.Page).GreaterThanOrEqualTo(1);
        RuleFor(i => i.PageSize).InclusiveBetween(1, 200);
    }
}
