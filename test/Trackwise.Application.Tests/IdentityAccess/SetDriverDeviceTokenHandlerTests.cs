using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.IdentityAccess.Employees.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.IdentityAccess;

public class SetDriverDeviceTokenHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ICurrentUserService currentUser = Substitute.For<ICurrentUserService>();
    private readonly List<Employee> employees = [];
    private readonly SetDriverDeviceTokenHandler sut;

    public SetDriverDeviceTokenHandlerTests()
    {
        tenantUow.Repository<Employee>().Query().Returns(_ => employees.BuildMock());
        tenantUow.Repository<Employee>().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(call => employees.SingleOrDefault(e => e.Id == call.Arg<Guid>()));
        sut = new SetDriverDeviceTokenHandler(tenantUow, currentUser);
    }

    private Employee AddDriver(string name, string? token = null)
    {
        var driver = new Employee
        {
            Code = name, Email = $"{name}@test.com", FirstName = name, LastName = "Driver", DeviceToken = token
        };
        employees.Add(driver);
        return driver;
    }

    [Fact]
    public async Task Handle_SamePhoneNewDriver_MovesTheTokenToThem()
    {
        var salvador = AddDriver("Salvador", "phone-1");
        var carlos = AddDriver("Carlos");
        currentUser.GetUserId().Returns(carlos.Id);

        var result = await sut.Handle(
            new SetDriverDeviceTokenCommand { UserId = carlos.Id, DeviceToken = "phone-1" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("phone-1", carlos.DeviceToken);
        Assert.Null(salvador.DeviceToken);
    }

    [Fact]
    public async Task Handle_SomeoneElsesId_Refused()
    {
        var salvador = AddDriver("Salvador", "phone-1");
        var carlos = AddDriver("Carlos");
        currentUser.GetUserId().Returns(carlos.Id);

        var result = await sut.Handle(
            new SetDriverDeviceTokenCommand { UserId = salvador.Id, DeviceToken = "phone-2" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("phone-1", salvador.DeviceToken);
    }
}
