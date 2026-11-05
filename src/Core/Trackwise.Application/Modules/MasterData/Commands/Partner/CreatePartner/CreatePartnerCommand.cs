using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Creates a business partner as a <b>draft</b>.
/// <para>
/// Nothing this command writes is live. A partner goes live when a second person approves it, which
/// is why there is no <c>IsActive</c> here to set - see <see cref="SubmitPartnerCommand"/> and
/// <see cref="ApprovePartnerCommand"/>. Creating and publishing being separate acts is the whole
/// content of maker-checker; a create that could also activate would make the rest decoration.
/// </para>
/// </summary>
public class CreatePartnerCommand : ICommand<Result<BusinessPartnerDto>>, IMasterDataWriteCommand
{
    /// <summary>
    /// Leave blank to have a code allocated from the <c>BusinessPartner</c> number series.
    /// Immutable afterwards - the accounting database and nSFS both join on it.
    /// </summary>
    public string? Code { get; set; }

    public string Name { get; set; } = default!;

    /// <summary>
    /// Every capacity this partner can act in. A company is routinely both the shipping line and
    /// the co-loader on different files, which is one record with two roles rather than two records.
    /// </summary>
    public List<BusinessPartnerRole> Roles { get; set; } = [];

    public Address? Address { get; set; }

    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>VAT ID / TIN. Stored digits-only, so the printed separators cannot hide a duplicate.</summary>
    public string? TaxId { get; set; }

    /// <summary>Set when this partner is also the billable customer on their files.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Owning branch, or null when every branch trades with this partner.</summary>
    public Guid? BranchId { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// company. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.BusinessPartner);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Code,

        // Normalised before probing, not after: the stored column is digits-only, so probing with
        // the "123-456-789-000" a steward copies off a BIR certificate would match nothing at all.
        ["TaxId"] = Domain.Entities.BusinessPartner.NormalizeTaxId(TaxId),
        ["Name"] = Name
    };
}
