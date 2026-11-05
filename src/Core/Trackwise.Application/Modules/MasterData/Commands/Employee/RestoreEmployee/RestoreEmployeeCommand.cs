using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Brings a soft-deleted employee back into ordinary queries.</summary>
public class RestoreEmployeeCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
