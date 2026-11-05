using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Brings a soft-deleted location back into ordinary queries.</summary>
public class RestoreLocationCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
