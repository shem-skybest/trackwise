using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using Trackwise.Application.Abstractions.Payments.Stripe;
using Address = Trackwise.Domain.Primitives.ValueObjects.Address;
using StripeAccount = Stripe.Account;
using StripeException = Stripe.StripeException;
using Trackwise.Application.Modules.Financial.Payroll.Commands;

namespace Trackwise.Application.Tests.Payroll;

public class SetupEmployeePayoutHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly IStripeConnectService stripeConnectService = Substitute.For<IStripeConnectService>();
    private readonly ILogger<SetupEmployeePayoutHandler> logger = NullLogger<SetupEmployeePayoutHandler>.Instance;

    private readonly ITenantRepository<Employee, Guid> employeeRepo =
        Substitute.For<ITenantRepository<Employee, Guid>>();

    private readonly SetupEmployeePayoutHandler sut;

    public SetupEmployeePayoutHandlerTests()
    {
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.GetCurrentTenant().Returns(CreateTenant());
        sut = new SetupEmployeePayoutHandler(tenantUow, stripeConnectService, logger);
    }

    private static Employee CreateEmployee(string? stripeAccountId = null)
    {
        return new Employee
        {
            Code = "JOHNDOE",
            Email = "driver@test.com",
            FirstName = "John",
            LastName = "Doe",
            StripeConnectedAccountId = stripeAccountId
        };
    }

    private static Tenant CreateTenant() => new()
    {
        Name = "test",
        ConnectionString = "test",
        BillingEmail = "billing@test.com",
        CompanyAddress = new Address
        {
            StreetAddress = "1 Test St",
            CityOrMunicipality = "Test",
            Province = "NA",
            Region = "",
            Zipcode = "00000",
            Country = "US"
        }
    };

    [Fact]
    public async Task Handle_NewEmployee_CreatesStripeAccount()
    {
        var employee = CreateEmployee();
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        stripeConnectService.CreateEmployeeConnectedAccountAsync(employee, Arg.Any<Address>())
            .Returns(new StripeAccount { Id = "acct_new_123" });

        var result = await sut.Handle(
            new SetupEmployeePayoutCommand { EmployeeId = employee.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("acct_new_123", employee.StripeConnectedAccountId);
        employeeRepo.Received(1).Update(employee);
        await tenantUow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadySetUp_ReturnsOkWithoutCreating()
    {
        var employee = CreateEmployee(stripeAccountId: "acct_existing");
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await sut.Handle(
            new SetupEmployeePayoutCommand { EmployeeId = employee.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await stripeConnectService.DidNotReceive()
            .CreateEmployeeConnectedAccountAsync(Arg.Any<Employee>(), Arg.Any<Address>());
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ReturnsFailure()
    {
        employeeRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Employee?)null);

        var result = await sut.Handle(
            new SetupEmployeePayoutCommand { EmployeeId = Guid.NewGuid() }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not find", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_StripeFailure_ReturnsError()
    {
        var employee = CreateEmployee();
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        stripeConnectService.CreateEmployeeConnectedAccountAsync(employee, Arg.Any<Address>())
            .ThrowsAsync(new StripeException("Account creation failed"));

        var result = await sut.Handle(
            new SetupEmployeePayoutCommand { EmployeeId = employee.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Account creation failed", result.Error!);
        Assert.Null(employee.StripeConnectedAccountId);
    }
}
