using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Webhooks.Commands;

public class ProcessResendWebhookCommand : ICommand<Result>
{
    public required string RawBody { get; set; }
    public string? SvixId { get; set; }
    public string? SvixTimestamp { get; set; }
    public string? SvixSignature { get; set; }
}
