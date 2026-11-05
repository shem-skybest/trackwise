using System.Linq.Expressions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class AddWorkfileContainersHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Workfile, Guid> workfileRepo = Substitute.For<ITenantRepository<Workfile, Guid>>();
    private readonly ITenantRepository<WorkfileContainer, Guid> slotRepo =
        Substitute.For<ITenantRepository<WorkfileContainer, Guid>>();
    private readonly ITenantRepository<ContainerType, Guid> typeRepo = Substitute.For<ITenantRepository<ContainerType, Guid>>();
    private readonly ITenantRepository<Container, Guid> boxRepo = Substitute.For<ITenantRepository<Container, Guid>>();

    private readonly RegularWorkfile workfile = RegularWorkfile.Create(
        "SLC-IMP0001-26-M", FreightMode.Import, Guid.NewGuid(), null,
        new WorkfileHeaderData { ReportCategory = ReportCategory.Psb });

    private readonly ContainerType hc40 = new() { Code = "HC40", Name = "40' High Cube", Category = ContainerTypeCategory.GeneralPurpose };
    private readonly ContainerType gp20 = new() { Code = "GP20", Name = "20' Dry", Category = ContainerTypeCategory.GeneralPurpose };

    private readonly AddWorkfileContainersHandler sut;

    public AddWorkfileContainersHandlerTests()
    {
        tenantUow.Repository<Workfile>().Returns(workfileRepo);
        tenantUow.Repository<WorkfileContainer>().Returns(slotRepo);
        tenantUow.Repository<ContainerType>().Returns(typeRepo);
        tenantUow.Repository<Container>().Returns(boxRepo);

        workfileRepo.GetByIdAsync(workfile.Id, Arg.Any<CancellationToken>()).Returns(workfile);
        typeRepo.GetByIdAsync(hc40.Id, Arg.Any<CancellationToken>()).Returns(hc40);
        typeRepo.GetByIdAsync(gp20.Id, Arg.Any<CancellationToken>()).Returns(gp20);

        sut = new AddWorkfileContainersHandler(tenantUow);
    }

    [Fact]
    public async Task Handle_QuantityWithoutNumber_AddsAndRegistersEachSlot()
    {
        var result = await sut.Handle(
            new AddWorkfileContainersCommand { WorkfileId = workfile.Id, ContainerTypeId = hc40.Id, Quantity = 3 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, workfile.Containers.Count);
        await slotRepo.Received(3).AddAsync(Arg.Any<WorkfileContainer>(), Arg.Any<CancellationToken>());
        await boxRepo.DidNotReceive().AddAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownNumber_AddsTheBoxToTheRegister()
    {
        boxRepo.GetAsync(Arg.Any<Expression<Func<Container, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Container?)null);

        var result = await sut.Handle(
            new AddWorkfileContainersCommand { WorkfileId = workfile.Id, ContainerTypeId = hc40.Id, ContainerNumber = "maeu7025097" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await boxRepo.Received(1).AddAsync(
            Arg.Is<Container>(c => c.Number == "MAEU7025097" && c.ContainerTypeId == hc40.Id),
            Arg.Any<CancellationToken>());
        Assert.NotNull(workfile.Containers.Single().ContainerId);
    }

    [Fact]
    public async Task Handle_KnownBoxOfAnotherSize_FailsNamingTheRegisteredSize()
    {
        var box = new Container { Number = "MAEU7025097", ContainerTypeId = gp20.Id };
        boxRepo.GetAsync(Arg.Any<Expression<Func<Container, bool>>>(), Arg.Any<CancellationToken>()).Returns(box);

        var result = await sut.Handle(
            new AddWorkfileContainersCommand { WorkfileId = workfile.Id, ContainerTypeId = hc40.Id, ContainerNumber = "MAEU7025097" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("GP20", result.Error);
        Assert.Empty(workfile.Containers);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveSize_Fails()
    {
        hc40.IsActive = false;

        var result = await sut.Handle(
            new AddWorkfileContainersCommand { WorkfileId = workfile.Id, ContainerTypeId = hc40.Id },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(workfile.Containers);
    }
}
