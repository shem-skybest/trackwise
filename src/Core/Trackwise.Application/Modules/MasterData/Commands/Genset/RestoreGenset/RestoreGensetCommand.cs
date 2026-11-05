using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Brings a soft-deleted genset back into ordinary queries.</summary>
public class RestoreGensetCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
