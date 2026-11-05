using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stripe;
using Xunit;
using Trackwise.Application.Abstractions.Payments.Stripe;
using Address = Trackwise.Domain.Primitives.ValueObjects.Address;
using Invoice = Trackwise.Domain.Entities.Invoice;
using Payment = Trackwise.Domain.Entities.Payment;
using Trackwise.Application.Modules.Financial.Payroll.Commands;

namespace Trackwise.Application.Tests.Payroll;

public class PayPayrollInvoiceHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly IStripeConnectService stripeConnectService = Substitute.For<IStripeConnectService>();
    private readonly ILogger<PayPayrollInvoiceHandler> logger = NullLogger<PayPayrollInvoiceHandler>.Instance;

    private readonly ITenantRepository<Invoice, Guid> invoiceRepo =
        Substitute.For<ITenantRepository<Invoice, Guid>>();

    private readonly ITenantRepository<Employee, Guid> employeeRepo =
        Substitute.For<ITenantRepository<Employee, Guid>>();

    private readonly ITenantRepository<Payment, Guid> paymentRepo =
        Substitute.For<ITenantRepository<Payment, Guid>>();

    private readonly PayPayrollInvoiceHandler sut;

    public PayPayrollInvoiceHandlerTests()
    {
        tenantUow.Repository<Invoice>().Returns(invoiceRepo);
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Payment>().Returns(paymentRepo);
        tenantUow.GetCurrentTenant().Returns(new Tenant
        {
            Name = "Test Tenant",
            ConnectionString = "test",
            BillingEmail = "test@test.com",
            CompanyAddress = new Address
            {
                StreetAddress = "123 Main St",
                CityOrMunicipality = "NYC",
                Province = "NY",
                Region = "",
                Zipcode = "10001",
                Country = "US"
            }
        });

        sut = new PayPayrollInvoiceHandler(tenantUow, stripeConnectService, logger);
    }

    private static PayrollInvoice CreateApprovedPayrollInvoice(Guid employeeId, decimal amount = 1500m)
    {
        var money = new Money { Amount = amount, Currency = "USD" };
        return new PayrollInvoice
        {
            EmployeeId = employeeId,
            Subtotal = money,
            TaxTotal = Money.Zero("USD"),
            Total = money,
            Status = InvoiceStatus.Approved,
            PeriodStart = DateTime.UtcNow.AddDays(-14),
            PeriodEnd = DateTime.UtcNow
        };
    }

    private static Employee CreateEmployee(string? stripeAccountId = "acct_123")
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

    [Fact]
    public async Task Handle_ApprovedInvoice_CreatesTransferAndPayment()
    {
        var employee = CreateEmployee();
        var invoice = CreateApprovedPayrollInvoice(employee.Id);

        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        stripeConnectService.CreateTransferAsync(
                150000, "USD", "acct_123", Arg.Any<string>())
            .Returns(new Transfer { Id = "tr_123" });

        var result = await sut.Handle(
            new PayPayrollInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await paymentRepo.Received(1).AddAsync(
            Arg.Is<Payment>(p =>
                p.Amount.Amount == 1500m &&
                p.Status == PaymentStatus.Paid &&
                p.StripePaymentIntentId == "tr_123"),
            Arg.Any<CancellationToken>());
        await tenantUow.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ReturnsFailure()
    {
        invoiceRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Invoice?)null);

        var result = await sut.Handle(
            new PayPayrollInvoiceCommand { InvoiceId = Guid.NewGuid() }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_DraftInvoice_ReturnsFailure()
    {
        var employee = CreateEmployee();
        var invoice = CreateApprovedPayrollInvoice(employee.Id);
        invoice.Status = InvoiceStatus.Draft;

        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        var result = await sut.Handle(
            new PayPayrollInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("approved", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_EmployeeWithoutStripeAccount_ReturnsFailure()
    {
        var employee = CreateEmployee(stripeAccountId: null);
        var invoice = CreateApprovedPayrollInvoice(employee.Id);

        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await sut.Handle(
            new PayPayrollInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("payout account", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_StripeTransferFails_ReturnsFailure()
    {
        var employee = CreateEmployee();
        var invoice = CreateApprovedPayrollInvoice(employee.Id);

        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);
        stripeConnectService.CreateTransferAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
            .ThrowsAsync(new StripeException("Insufficient funds"));

        var result = await sut.Handle(
            new PayPayrollInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Insufficient funds", result.Error!);
        await paymentRepo.DidNotReceive().AddAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FullyPaidInvoice_ReturnsFailure()
    {
        var employee = CreateEmployee();
        var invoice = CreateApprovedPayrollInvoice(employee.Id);
        // Add a payment that covers the full amount
        invoice.Payments.Add(new Payment
        {
            Amount = new Money { Amount = 1500m, Currency = "USD" },
            Status = PaymentStatus.Paid,
            TenantId = Guid.NewGuid(),
            BillingAddress = new Address
            {
                StreetAddress = "N/A",
                CityOrMunicipality = "N/A",
                Province = "N/A",
                Region = "",
                Zipcode = "00000",
                Country = "US"
            }
        });

        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);
        employeeRepo.GetByIdAsync(employee.Id, Arg.Any<CancellationToken>()).Returns(employee);

        var result = await sut.Handle(
            new PayPayrollInvoiceCommand { InvoiceId = invoice.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("fully paid", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
