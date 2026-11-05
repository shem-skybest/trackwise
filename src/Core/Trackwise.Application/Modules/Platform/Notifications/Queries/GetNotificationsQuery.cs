using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Notifications.Queries;

public class GetNotificationsQuery : IntervalQuery, IQuery<Result<NotificationDto[]>>
{
}
