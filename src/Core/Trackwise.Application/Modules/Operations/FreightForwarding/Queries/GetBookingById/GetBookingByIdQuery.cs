using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

public class GetBookingByIdQuery : IQuery<Result<BookingDto>>
{
    public Guid Id { get; set; }
}
