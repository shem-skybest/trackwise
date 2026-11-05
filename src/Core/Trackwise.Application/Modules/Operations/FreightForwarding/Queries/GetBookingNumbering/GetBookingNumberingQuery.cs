using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>This year's BKN-RFV counter and the number the next Cold Chain booking will get.</summary>
public class GetBookingNumberingQuery : IQuery<Result<BookingNumberingDto>>;
