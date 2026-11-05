using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>Creates a helper. The first record a helper has ever had in this domain.</summary>
public class CreateHelperCommand : ICommand<Result<HelperDto>>, IMasterDataWriteCommand
{
    /// <summary>Leave blank to have a code allocated from the <c>Helper</c> number series.</summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;
    public string? PhoneNumber { get; set; }
    public Address? Address { get; set; }
    public string? Notes { get; set; }

    /// <summary>Regular or on-call; null when not recorded.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>The unit they normally work for (Cold Chain, wing van, trucking); null when not recorded.</summary>
    public CrewUnit? HomeUnit { get; set; }

    /// <summary>Owning branch, or null when every branch uses this helper.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// person. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Helper);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,
        ["Name"] = Name
    };
}
