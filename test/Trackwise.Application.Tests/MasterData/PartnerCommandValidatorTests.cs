using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

public class PartnerCommandValidatorTests
{
    private readonly CreatePartnerValidator create = new();
    private readonly UpdatePartnerValidator update = new();
    private readonly RejectPartnerValidator reject = new();

    private static CreatePartnerCommand Valid() => new()
    {
        Name = "Maersk Filipinas Inc.",
        Roles = [BusinessPartnerRole.ShippingLine]
    };

    [Fact]
    public void Create_WithoutACode_IsValid()
    {
        // Blank means "allocate one from the BusinessPartner series", the same bargain every other
        // master entity strikes. The old freight-forwarding command required one.
        Assert.True(create.Validate(Valid()).IsValid);
    }

    [Fact]
    public void Create_WithNoRoles_IsRejected()
    {
        // A partner with no role can never be picked for a workfile slot, so it is a dead record
        // rather than an incomplete one.
        var command = Valid();
        command.Roles = [];

        Assert.False(create.Validate(command).IsValid);
    }

    [Theory]
    [InlineData("123-456-789-000")]
    [InlineData("123 456 789 000")]
    [InlineData("TIN: 123456789000")]
    public void Create_AcceptsATinInWhateverFormTheCertificatePrintsIt(string taxId)
    {
        // Validated on the normalised value, because that is what gets stored. Rejecting the dashes
        // would teach a steward to strip them by hand on every record.
        var command = Valid();
        command.TaxId = taxId;

        Assert.True(create.Validate(command).IsValid);
    }

    [Fact]
    public void Create_WithATinThatHasNoDigits_IsRejected()
    {
        var command = Valid();
        command.TaxId = "not applicable";

        Assert.False(create.Validate(command).IsValid);
    }

    [Fact]
    public void Create_OverridingTheDuplicateProbeWithoutAReason_IsRejected()
    {
        // The override is allowed, but never silently: the reason is what a reviewer reads months
        // later when the duplicate turns out to have been real.
        var command = Valid();
        command.ConfirmedDistinct = true;

        Assert.False(create.Validate(command).IsValid);

        command.DistinctReason = "Different subsidiary, separate TIN.";
        Assert.True(create.Validate(command).IsValid);
    }

    [Fact]
    public void Update_DeactivatingOverDependentsWithoutAReason_IsRejected()
    {
        var command = new UpdatePartnerCommand
        {
            Id = Guid.NewGuid(),
            Name = "Maersk Filipinas Inc.",
            Roles = [BusinessPartnerRole.ShippingLine],
            IsActive = false,
            ConfirmedDespiteDependents = true
        };

        Assert.False(update.Validate(command).IsValid);

        command.DependencyReason = "Account closed; the open files are being reassigned.";
        Assert.True(update.Validate(command).IsValid);
    }

    [Fact]
    public void Update_ConfirmingDependentsWhileStayingActive_NeedsNoReason()
    {
        // The flag is meaningless on a write that retires nothing, so it must not demand a reason
        // for a decision nobody is making.
        var command = new UpdatePartnerCommand
        {
            Id = Guid.NewGuid(),
            Name = "Maersk Filipinas Inc.",
            Roles = [BusinessPartnerRole.ShippingLine],
            IsActive = true,
            ConfirmedDespiteDependents = true
        };

        Assert.True(update.Validate(command).IsValid);
    }

    [Fact]
    public void Reject_WithoutAReason_IsRejected()
    {
        // A record sent back without a reason is a record the steward resubmits unchanged, and the
        // round trip has taught nobody anything.
        var command = new RejectPartnerCommand { Id = Guid.NewGuid(), Reason = "" };

        Assert.False(reject.Validate(command).IsValid);
    }
}
