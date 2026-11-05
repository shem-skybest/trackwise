using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Brings a soft-deleted branch back into ordinary queries.</summary>
public class RestoreBranchCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
