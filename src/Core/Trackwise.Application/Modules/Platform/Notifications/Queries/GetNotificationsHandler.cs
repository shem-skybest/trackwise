using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notifications.Queries;

internal sealed class GetNotificationsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetNotificationsQuery, Result<NotificationDto[]>>
{
    public async Task<Result<NotificationDto[]>> Handle(
        GetNotificationsQuery req,
        CancellationToken ct)
    {
        var notificationsList =
            await tenantUow.Repository<Notification>()
                .GetListAsync(i => i.CreatedDate >= req.StartDate && i.CreatedDate <= req.EndDate, ct);

        var notificationsDto = notificationsList
            .Select(i => i.ToDto())
            .OrderByDescending(i => i.CreatedDate)
            .ToArray();

        return Result<NotificationDto[]>.Ok(notificationsDto);
    }
}
