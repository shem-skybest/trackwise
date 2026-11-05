using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

public sealed class ResetTenantQuotasCommand : ICommand<Result>
{
    public List<Guid> TenantIds { get; init; } = [];
}
