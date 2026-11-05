using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Brings a soft-deleted business partner back into ordinary queries.</summary>
public class RestorePartnerCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
