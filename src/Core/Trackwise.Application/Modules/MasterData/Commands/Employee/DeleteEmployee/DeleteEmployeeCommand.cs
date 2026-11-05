using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Soft-deletes an employee. The row survives so existing loads, licenses and payroll history keep
/// resolving; it simply stops appearing in ordinary queries. Reversed by
/// <see cref="RestoreEmployeeCommand" />.
/// <para>
/// <see cref="ConfirmedDespiteDependents"/>/<see cref="DependencyReason"/> gate two different things
/// for this entity, not just the usual dependency probe: an employee whose id still belongs to an
/// active platform login (see the handler) needs the same explicit confirmation, since soft-deleting
/// strips that person's tenant access as a side effect.
/// </para>
/// </summary>
public class DeleteEmployeeCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand
{
    public Guid Id { get; set; }

    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Employee);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation => MasterDataDeactivation.Delete;
}
