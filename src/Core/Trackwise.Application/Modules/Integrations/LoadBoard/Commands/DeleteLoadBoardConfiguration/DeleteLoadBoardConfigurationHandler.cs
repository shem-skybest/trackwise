using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Commands;

internal sealed class DeleteLoadBoardConfigurationHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeleteLoadBoardConfigurationCommand, LoadBoardConfiguration>(tenantUow);
