using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Application.Modules.Integrations.Webhooks.Commands;
using Trackwise.Application.Tests.TestKit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Xunit;

namespace Trackwise.Application.Tests.Webhooks;

public class ProcessXenditCallbackHandlerTests
{
    private const string SecretKey = "xnd_development_secret";
    private const string CallbackToken = "callback-token-123";
    private const string XenditInvoiceId = "65f0c0ffee";

    private readonly IMasterUnitOfWork masterUow = Substitute.For<IMasterUnitOfWork>();
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly IXenditClient xendit = Substitute.For<IXenditClient>();

    private readonly IMasterRepository<Tenant, Guid> tenantRepo = Substitute.For<IMasterRepository<Tenant, Guid>>();
    private readonly ITenantRepository<PaymentGatewayConfiguration, Guid> configRepo =
        Substitute.For<ITenantRepository<PaymentGatewayConfiguration, Guid>>();
    private readonly ITenantRepository<Payment, Guid> paymentRepo = Substitute.For<ITenantRepository<Payment, Guid>>();
    private readonly ITenantRepository<Invoice, Guid> invoiceRepo = Substitute.For<ITenantRepository<Invoice, Guid>>();

    private readonly Tenant tenant = TestTenant.Create("Skybest");
    private readonly LoadInvoice invoice;
    private readonly ProcessXenditCallbackHandler sut;

    public ProcessXenditCallbackHandlerTests()
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

        tenantUow.Repository<PaymentGatewayConfiguration>().Returns(configRepo);
        tenantUow.Repository<Payment>().Returns(paymentRepo);
        tenantUow.Repository<Invoice>().Returns(invoiceRepo);

        configRepo.GetAsync(Arg.Any<Expression<Func<PaymentGatewayConfiguration, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentGatewayConfiguration
            {
                Gateway = PaymentGateway.Xendit,
                SecretKey = SecretKey,
                CallbackToken = CallbackToken
            });
        paymentRepo.GetAsync(Arg.Any<Expression<Func<Payment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns((Payment?)null);
        invoiceRepo.GetByIdAsync(invoice.Id, Arg.Any<CancellationToken>()).Returns(invoice);

        SetupXenditInvoice("PAID");

        sut = new ProcessXenditCallbackHandler(
            masterUow, tenantUow, xendit, NullLogger<ProcessXenditCallbackHandler>.Instance);
    }

    private void SetupXenditInvoice(string status, decimal? paidAmount = 1000m, string? externalId = null) =>
        xendit.GetInvoiceAsync(SecretKey, XenditInvoiceId, Arg.Any<CancellationToken>())
            .Returns(new XenditInvoice
            {
                Id = XenditInvoiceId,
                ExternalId = externalId ?? $"INV42-{invoice.Id:N}",
                Status = status,
                Amount = 1000m,
                PaidAmount = paidAmount,
                Currency = "PHP",
                InvoiceUrl = "https://checkout-staging.xendit.co/web/x",
                PaymentChannel = "GCASH"
            });

    private ProcessXenditCallbackCommand Command(string? token = CallbackToken, string? body = null) => new()
    {
        TenantId = tenant.Id,
        CallbackToken = token,
        RawBody = body ?? $$"""{ "id": "{{XenditInvoiceId}}", "status": "PAID", "amount": 1 }"""
    };

    [Fact]
    public async Task PaidInvoice_RecordsPaymentFromXenditNotFromBody()
    {
        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // The body claimed 1 peso; the recorded amount comes from Xendit's own answer.
        await paymentRepo.Received(1).AddAsync(
            Arg.Is<Payment>(p =>
                p.Amount.Amount == 1000m
                && p.Gateway == PaymentGateway.Xendit
                && p.GatewayReference == XenditInvoiceId
                && p.Status == PaymentStatus.Paid),
            Arg.Any<CancellationToken>());
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PartialAmount_MarksInvoicePartiallyPaid()
    {
        SetupXenditInvoice("PAID", paidAmount: 400m);

        await sut.Handle(Command(), CancellationToken.None);

        Assert.Equal(InvoiceStatus.PartiallyPaid, invoice.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-token")]
    public async Task BadToken_IsRejectedWithoutCallingXendit(string? token)
    {
        var result = await sut.Handle(Command(token), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.WebhookRejected, result.ErrorCode);
        await xendit.DidNotReceiveWithAnyArgs().GetInvoiceAsync(default!, default!, default);
    }

    [Fact]
    public async Task UnknownTenant_IsRejected()
    {
        var result = await sut.Handle(
            new ProcessXenditCallbackCommand { TenantId = Guid.NewGuid(), CallbackToken = CallbackToken, RawBody = "{}" },
            CancellationToken.None);

        Assert.Equal(ErrorCodes.WebhookRejected, result.ErrorCode);
    }

    [Fact]
    public async Task AlreadyRecorded_IsNoOp()
    {
        paymentRepo.GetAsync(Arg.Any<Expression<Func<Payment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new Payment
            {
                Amount = new Money { Amount = 1000m, Currency = "PHP" },
                TenantId = tenant.Id,
                BillingAddress = tenant.CompanyAddress
            });

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await paymentRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Theory]
    [InlineData("PENDING")]
    [InlineData("EXPIRED")]
    public async Task NotPaid_RecordsNothing(string status)
    {
        SetupXenditInvoice(status);

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await paymentRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task SettledCountsAsPaid()
    {
        SetupXenditInvoice("SETTLED");

        await sut.Handle(Command(), CancellationToken.None);

        await paymentRepo.ReceivedWithAnyArgs(1).AddAsync(default!, default);
    }

    [Fact]
    public async Task ForeignExternalId_IsIgnored()
    {
        SetupXenditInvoice("PAID", externalId: "shopify-order-991");

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await paymentRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task XenditDown_FailsSoXenditRetries()
    {
        xendit.GetInvoiceAsync(SecretKey, XenditInvoiceId, Arg.Any<CancellationToken>())
            .Throws(new XenditApiException("timeout"));

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.NotEqual(ErrorCodes.WebhookRejected, result.ErrorCode);
    }

    [Fact]
    public async Task DashboardTestCallback_UnknownToXendit_IsAccepted()
    {
        xendit.GetInvoiceAsync(SecretKey, XenditInvoiceId, Arg.Any<CancellationToken>())
            .Throws(new XenditApiException("not found", 404));

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
