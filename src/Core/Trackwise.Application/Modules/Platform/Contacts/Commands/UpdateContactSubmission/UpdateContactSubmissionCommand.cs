using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Contacts.Commands;

public sealed class UpdateContactSubmissionCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public ContactSubmissionStatus Status { get; set; }
    public string? Notes { get; set; }
}
