using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Privacy.Commands;

public class RecordConsentCommand : ICommand<Result<Guid>>
{
    public Guid UserId { get; set; }
    public ConsentType ConsentType { get; set; }
    public bool Granted { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
