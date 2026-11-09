using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ConvertQuotationToBooking;

public class ConvertQuotationToBookingCommand : ICommand<Result<Guid>>
{
    public Guid QuotationId { get; set; }
    public Guid? ContainerTypeId { get; set; }
    public DateTime? RequestedPickupDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public bool Repeat { get; set; }
}
