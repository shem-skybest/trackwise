using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Brings a soft-deleted vessel back into ordinary queries.</summary>
public class RestoreVesselCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
