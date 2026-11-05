using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Compliance.Eld.Commands;

public class ProcessEldWebhookCommand : ICommand
{
    public EldProviderType ProviderType { get; set; }
    public required string RequestBodyJson { get; set; }
    public string? Signature { get; set; }
}
