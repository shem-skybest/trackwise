using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Application.Modules.Financial.Payments.Commands;
using Trackwise.Application.Tests.TestKit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Xunit;

namespace Trackwise.Application.Tests.Payments;

public class ConfirmPublicPaymentHandlerTests
{
    private const string SecretKey = "xnd_development_secret";
    private const string Token = "link-token";

    private readonly IMasterUnitOfWork masterUow = Substitute.For<IMasterUnitOfWork>();
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly IXenditClient xendit = Substitute.For<IXenditClient>();

    private readonly IMasterRepository<Tenant, Guid> tenantRepo = Substitute.For<IMasterRepository<Tenant, Guid>>();
    private readonly ITenantRepository<PaymentLink, Guid> linkRepo = Substitute.For<ITenantRepository<PaymentLink, Guid>>();
    private readonly ITenantRepository<PaymentGatewayConfiguration, Guid> configRepo =
        Substitute.For<ITenantRepository<PaymentGatewayConfiguration, Guid>>();
    private readonly ITenantRepository<Payment, Guid> paymentRepo = Substitute.For<ITenantRepository<Payment, Guid>>();
    private readonly ITenantRepository<Invoice, Guid> invoiceRepo = Substitute.For<ITenantRepository<Invoice, Guid>>();

    private readonly Tenant tenant = TestTenant.Create("Skybest");
    private readonly LoadInvoice invoice;
    private readonly ConfirmPublicPaymentHandler sut;

    public ConfirmPublicPaymentHandlerTests()
    {
        invoice = new LoadInvoice
        {
            Number = 42,
            LoadId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Subtotal = new Money { Amount = 1000m, Currency = "PHP" },
            TaxTotal = Money.Zero("PHP"),
            Total = new Money { Amount = 1000m, Currency = "PHP" },
            Status = InvoiceStatus.Issued
        };

        masterUow.Repository<Tenant>().Returns(tenantRepo);
        tenantRepo.GetByIdAsync(tenant.Id, Arg.Any<CancellationToken>()).Returns(tenant);

        tenantUow.Repository<PaymentLink>().Returns(linkRepo);
        tenantUow.Repository<PaymentGatewayConfiguration>().Returns(configRepo);
        tenantUow.Repository<Payment>().Returns(paymentRepo);
        tenantUow.Repository<Invoice>().Returns(invoiceRepo);

        linkRepo.GetAsync(Arg.Any<Expression<Func<PaymentLink, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentLink
            {
                Token = Token,
                InvoiceId = invoice.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByUserId = Guid.NewGuid()
            });
        configRepo.GetAsync(Arg.Any<Expression<Func<PaymentGatewayConfiguration, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayConfiguration
            {
                Gateway = PaymentGateway.Xendit,
                SecretKey = SecretKey,
                CallbackToken = "cb"
            });
        paymentRepo.GetAsync(Arg.Any<Expression<Func<Payment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        sut = new ConfirmPublicPaymentHandler(
            masterUow, tenantUow, xendit, NullLogger<ConfirmPublicPaymentHandler>.Instance);
    }

    private void SetupAttempts(params (string Id, string Status)[] attempts) =>
        xendit.ListInvoicesAsync(SecretKey, $"INV42-{invoice.Id:N}", Arg.Any<CancellationToken>())
            .Returns(attempts.Select(a => new XenditInvoice
            {
                Id = a.Id,
                ExternalId = $"INV42-{invoice.Id:N}",
                Status = a.Status,
                Amount = 1000m,
                PaidAmount = a.Status == "PAID" ? 1000m : null,
                Currency = "PHP",
                InvoiceUrl = "https://checkout-staging.xendit.co/web/x"
            }).ToList());

    private Task<Result<PublicPaymentConfirmationDto>> Confirm() =>
        sut.Handle(new ConfirmPublicPaymentCommand { TenantId = tenant.Id, Token = Token }, CancellationToken.None);

    [Fact]
    public async Task PaidAttempt_MissedByCallback_IsRecorded()
    {
        SetupAttempts(("x-expired", "EXPIRED"), ("x-paid", "PAID"));

        var result = await Confirm();

        Assert.Equal(PublicPaymentState.Paid, result.Value!.State);
        await paymentRepo.Received(1).AddAsync(
            Arg.Is<Payment>(p => p.GatewayReference == "x-paid" && p.Amount.Amount == 1000m),
            Arg.Any<CancellationToken>());
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public async Task AlreadyRecordedByCallback_IsPaidWithoutDuplicate()
    {
        SetupAttempts(("x-paid", "PAID"));
        paymentRepo.GetAsync(Arg.Any<Expression<Func<Payment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new Payment
            {
                Amount = new Money { Amount = 1000m, Currency = "PHP" },
                TenantId = tenant.Id,
                BillingAddress = tenant.CompanyAddress
            });

        var result = await Confirm();

        Assert.Equal(PublicPaymentState.Paid, result.Value!.State);
        await paymentRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task OnlyPendingAttempt_IsPending()
    {
        SetupAttempts(("x-otc", "PENDING"));

        var result = await Confirm();

        Assert.Equal(PublicPaymentState.Pending, result.Value!.State);
        await paymentRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task NoAttempts_IsNotPaid()
    {
        SetupAttempts();

        var result = await Confirm();

        Assert.Equal(PublicPaymentState.NotPaid, result.Value!.State);
    }

    [Fact]
    public async Task XenditDown_IsPending()
    {
        xendit.ListInvoicesAsync(default!, default!, default)
            .ReturnsForAnyArgs<IReadOnlyList<XenditInvoice>>(_ => throw new XenditApiException("timeout"));

        var result = await Confirm();

        Assert.True(result.IsSuccess);
        Assert.Equal(PublicPaymentState.Pending, result.Value!.State);
    }

    [Fact]
    public async Task NoXenditAccount_IsNotChecked()
    {
        configRepo.GetAsync(Arg.Any<Expression<Func<PaymentGatewayConfiguration, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentGatewayConfiguration?)null);

        var result = await Confirm();

        Assert.Equal(PublicPaymentState.NotChecked, result.Value!.State);
        await xendit.DidNotReceiveWithAnyArgs().ListInvoicesAsync(default!, default!, default);
    }

    [Fact]
    public async Task UnknownLink_Fails()
    {
        linkRepo.GetAsync(Arg.Any<Expression<Func<PaymentLink, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentLink?)null);

        var result = await Confirm();

        Assert.False(result.IsSuccess);
        await xendit.DidNotReceiveWithAnyArgs().ListInvoicesAsync(default!, default!, default);
    }
}
