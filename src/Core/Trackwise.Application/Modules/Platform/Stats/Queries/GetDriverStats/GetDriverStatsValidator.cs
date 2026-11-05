using FluentValidation;

namespace Trackwise.Application.Modules.Platform.Stats.Queries;

internal class GetDriverStatsValidator : AbstractValidator<GetDriverStatsQuery>
{
    public GetDriverStatsValidator()
    {
        RuleFor(i => i.UserId).NotEmpty();
    }
}
