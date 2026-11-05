using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Integrations.Webhooks.Commands;

public class ProcessStripEventCommand : ICommand
{
    public string? RequestBodyJson { get; set; }
    public string? StripeSignature { get; set; }
}
