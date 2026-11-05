using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public class ConfirmLoadStatus
{
    public Guid? DriverId { get; set; }
    public Guid? LoadId { get; set; }
    public LoadStatus LoadStatus { get; set; }
}
