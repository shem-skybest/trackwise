using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// What a workfile is allowed to name.
/// <para>
/// The create and update handlers these used to cover are gone: partners became master data, and
/// their writes moved to <c>/master-data/partners</c> where they go through the duplicate probe,
/// the dependency probe, the change log and approval. What survives here is the entity's own rule,
/// which is the one every one of those paths ultimately protects.
/// </para>
/// </summary>
public class BusinessPartnerTests
{
    private static BusinessPartner Approved() => new()
    {
        Name = "Maersk Line",
        Code = "MAEU",
        Roles = [BusinessPartnerRole.ShippingLine],
        ApprovalState = MasterDataApprovalState.Approved,
        IsActive = true
    };

    [Fact]
    public void CanActAs_MatchingRoleOnActivePartner_IsAllowed()
    {
        Assert.True(Approved().CanActAs(BusinessPartnerRole.ShippingLine));
    }

    [Fact]
    public void CanActAs_RoleThePartnerDoesNotHold_IsRefused()
    {
        Assert.False(Approved().CanActAs(BusinessPartnerRole.Broker));
    }

    [Fact]
    public void CanActAs_InactivePartner_IsRefusedEvenWithTheRole()
    {
        // A workfile must not name a partner ops has retired, however it was spelled when picked.
        var partner = Approved();
        partner.IsActive = false;

        Assert.False(partner.CanActAs(BusinessPartnerRole.ShippingLine));
    }

    [Fact]
    public void CanActAs_DraftPartner_IsRefusedBecauseADraftIsNeverActive()
    {
        // The pairing that makes maker-checker real rather than decorative: a partner nobody has
        // approved is not active, and CanActAs already refused inactive partners - so no workfile
        // code had to learn about approval at all.
        var partner = new BusinessPartner
        {
            Name = "New Co",
            Code = "NEWCO",
            Roles = [BusinessPartnerRole.Broker]
        };

        Assert.Equal(MasterDataApprovalState.Draft, partner.ApprovalState);
        Assert.False(partner.IsActive);
        Assert.False(partner.CanActAs(BusinessPartnerRole.Broker));
    }

    [Fact]
    public void CanActAs_MergedTombstone_IsRefusedEvenWhileItStillLooksActive()
    {
        // A merged record keeps its flags until the merge soft-deletes it, and callers that read
        // past the query filter can hold one. Attaching a tombstone to a live file would point that
        // file at a record the survivor has already taken every reference from.
        var partner = Approved();
        partner.MergedIntoId = Guid.NewGuid();

        Assert.False(partner.CanActAs(BusinessPartnerRole.ShippingLine));
    }

    [Theory]
    [InlineData("123-456-789-000", "123456789000")]
    [InlineData(" 123 456 789 ", "123456789")]
    [InlineData("TIN 001", "001")]
    public void NormalizeTaxId_StripsEverythingButDigits(string input, string expected)
    {
        // The duplicate probe matches TINs exactly, so the punctuation a certificate prints them
        // with is the difference between catching a duplicate and not.
        Assert.Equal(expected, BusinessPartner.NormalizeTaxId(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("n/a")]
    public void NormalizeTaxId_WithNoDigits_IsNullRatherThanEmpty(string? input)
    {
        // An empty string would be a value every partner without a TIN shares, and an exact rule
        // would then match all of them against each other.
        Assert.Null(BusinessPartner.NormalizeTaxId(input));
    }
}
