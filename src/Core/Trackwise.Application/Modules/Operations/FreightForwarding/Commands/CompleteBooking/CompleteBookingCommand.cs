using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

public class CompleteBookingCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
