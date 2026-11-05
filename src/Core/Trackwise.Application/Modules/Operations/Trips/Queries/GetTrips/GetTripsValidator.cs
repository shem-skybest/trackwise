using FluentValidation;
using Trackwise.Application.Modules.IdentityAccess.Tenants.Queries;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripsValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTripsValidator()
    {
        RuleFor(i => i.Page)
            .GreaterThanOrEqualTo(0);

        RuleFor(i => i.PageSize)
            .GreaterThanOrEqualTo(1);
    }
}
