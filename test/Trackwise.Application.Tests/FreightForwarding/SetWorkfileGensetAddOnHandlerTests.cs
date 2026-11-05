using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class SetWorkfileGensetAddOnHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo =
        Substitute.For<ITenantRepository<Workfile, Guid>>();
    private readonly ITenantRepository<Genset, Guid> gensetRepo =
        Substitute.For<ITenantRepository<Genset, Guid>>();

    private readonly ReeferWorkfile workfile;
    private readonly SetWorkfileGensetAddOnHandler sut;

    public SetWorkfileGensetAddOnHandlerTests()
    {
        workfile = ReeferWorkfile.Create(
            "BKN-RFV-000001", Guid.NewGuid(), null,
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });

        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        tenantUow.Repository<Genset>().Returns(gensetRepo);
        workfileRepo.GetByIdAsync(workfile.Id, Arg.Any<CancellationToken>()).Returns(workfile);

        sut = new SetWorkfileGensetAddOnHandler(tenantUow);
    }

    [Fact]
    public async Task Handle_ProfitShareWithNoUnitAssignedYet_Succeeds()
    {
        var result = await sut.Handle(
            new SetWorkfileGensetAddOnCommand
            {
                Id = workfile.Id,
                BillingType = GensetBillingType.ProfitShare,
                FlatAmount = 2000m,
                Notes = "SULONG fronting"
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GensetBillingType.ProfitShare, workfile.GensetBillingType);
        Assert.Null(workfile.GensetId);
    }

    [Fact]
    public async Task Handle_ActiveAssignedGenset_Succeeds()
    {
        var genset = new Genset { Code = "G1", Name = "G1", Status = GensetStatus.Available };
        gensetRepo.GetByIdAsync(genset.Id, Arg.Any<CancellationToken>()).Returns(genset);

        var result = await sut.Handle(
            new SetWorkfileGensetAddOnCommand
            {
                Id = workfile.Id,
                GensetId = genset.Id,
                BillingType = GensetBillingType.Itemized,
                HourlyRate = 350m
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(genset.Id, workfile.GensetId);
    }

    [Fact]
    public async Task Handle_UnknownGensetId_Fails()
    {
        gensetRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Genset?)null);

        var result = await sut.Handle(
            new SetWorkfileGensetAddOnCommand
            {
                Id = workfile.Id,
                GensetId = Guid.NewGuid(),
                BillingType = GensetBillingType.ProfitShare,
                FlatAmount = 2000m
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(workfile.GensetBillingType);
    }

    [Fact]
    public async Task Handle_InactiveGenset_Fails()
    {
        var genset = new Genset
        {
            Code = "G2", Name = "G2", Status = GensetStatus.OutOfService, IsActive = false
        };
        gensetRepo.GetByIdAsync(genset.Id, Arg.Any<CancellationToken>()).Returns(genset);

        var result = await sut.Handle(
            new SetWorkfileGensetAddOnCommand
            {
                Id = workfile.Id,
                GensetId = genset.Id,
                BillingType = GensetBillingType.ProfitShare,
                FlatAmount = 2000m
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NonReeferFile_Fails()
    {
        var regular = RegularWorkfile.Create(
            "x", FreightMode.Import, Guid.NewGuid(), Guid.NewGuid(),
            new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });
        workfileRepo.GetByIdAsync(regular.Id, Arg.Any<CancellationToken>()).Returns(regular);

        var result = await sut.Handle(
            new SetWorkfileGensetAddOnCommand
            {
                Id = regular.Id,
                BillingType = GensetBillingType.ProfitShare,
                FlatAmount = 2000m
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_UnknownWorkfile_Fails()
    {
        var result = await sut.Handle(
            new SetWorkfileGensetAddOnCommand
            {
                Id = Guid.NewGuid(),
                BillingType = GensetBillingType.ProfitShare,
                FlatAmount = 2000m
            },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
