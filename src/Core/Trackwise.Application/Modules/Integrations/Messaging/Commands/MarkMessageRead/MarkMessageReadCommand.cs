using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Messaging.Commands;

public class MarkMessageReadCommand : ICommand
{
    public Guid MessageId { get; set; }
    public Guid ReadById { get; set; }
}
