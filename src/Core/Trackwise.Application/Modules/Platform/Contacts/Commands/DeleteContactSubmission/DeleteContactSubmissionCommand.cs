using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Contacts.Commands;

public sealed class DeleteContactSubmissionCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
