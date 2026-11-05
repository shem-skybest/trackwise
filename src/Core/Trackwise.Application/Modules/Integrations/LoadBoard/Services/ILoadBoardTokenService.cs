using Trackwise.Application.Abstractions.Common;
using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.LoadBoard;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Services;

/// <summary>
/// Returns a provider whose OAuth token is valid, persisting refreshed tokens on the tenant
/// configuration. Saves the scope immediately on acquisition - call before staging other changes.
/// </summary>
public interface ILoadBoardTokenService : IApplicationService
{
    Task<Result<ILoadBoardProviderService>> GetReadyProviderAsync(
        LoadBoardConfiguration configuration, CancellationToken ct = default);
}
