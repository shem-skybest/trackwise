using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// <see cref="RecordWorkfileGensetHoursHandler"/> and <see cref="RemoveWorkfileGensetAddOnHandler"/>
/// - the two verbs the entity-level rules in <see cref="WorkfileGensetAddOnTests"/> back onto a
/// handler. Covers only the not-found/pass-through paths; the business rules are the entity's.
/// </summary>
public class WorkfileGensetHandlersTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo =
        Substitute.For<ITenantRepository<Workfile, Guid>>();

    private readonly ReeferWorkfile workfile;

    public WorkfileGensetHandlersTests()
    {
        workfile = ReeferWorkfile.Create(
            "BKN-RFV-000001", Guid.NewGuid(), null,
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });

        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        workfileRepo.GetByIdAsync(workfile.Id, Arg.Any<CancellationToken>()).Returns(workfile);
    }

    [Fact]
    public async Task RecordHours_OnAnItemizedAddOn_UpdatesTheEntity()
    {
        workfile.SetGensetAddOn(null, GensetBillingType.Itemized, 350m, null, null);

        var result = await new RecordWorkfileGensetHoursHandler(tenantUow).Handle(
            new RecordWorkfileGensetHoursCommand { Id = workfile.Id, HoursUsed = 5m },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, workfile.GensetHoursUsed);
    }

    [Fact]
    public async Task RecordHours_WithNoAddOn_FailsInsteadOfThrowing()
    {
        var result = await new RecordWorkfileGensetHoursHandler(tenantUow).Handle(
            new RecordWorkfileGensetHoursCommand { Id = workfile.Id, HoursUsed = 5m },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RecordHours_UnknownWorkfile_Fails()
    {
        var result = await new RecordWorkfileGensetHoursHandler(tenantUow).Handle(
            new RecordWorkfileGensetHoursCommand { Id = Guid.NewGuid(), HoursUsed = 5m },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Remove_WithAnExistingAddOn_ClearsIt()
    {
        workfile.SetGensetAddOn(null, GensetBillingType.ProfitShare, null, 2000m, null);

        var result = await new RemoveWorkfileGensetAddOnHandler(tenantUow).Handle(
            new RemoveWorkfileGensetAddOnCommand { Id = workfile.Id },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(workfile.GensetBillingType);
    }

    [Fact]
    public async Task Remove_WithNoAddOn_FailsInsteadOfThrowing()
    {
        var result = await new RemoveWorkfileGensetAddOnHandler(tenantUow).Handle(
            new RemoveWorkfileGensetAddOnCommand { Id = workfile.Id },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Remove_UnknownWorkfile_Fails()
    {
        var result = await new RemoveWorkfileGensetAddOnHandler(tenantUow).Handle(
            new RemoveWorkfileGensetAddOnCommand { Id = Guid.NewGuid() },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
