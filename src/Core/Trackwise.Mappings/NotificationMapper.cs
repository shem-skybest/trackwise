using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Mappings;

public static class NotificationMapper
{
    public static NotificationDto ToDto(this Notification entity)
    {
        return new NotificationDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Message = entity.Message,
            IsRead = entity.IsRead,
            CreatedDate = entity.CreatedDate
        };
    }
}
