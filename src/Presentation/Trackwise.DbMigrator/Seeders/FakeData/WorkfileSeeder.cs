using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Promotes the InExecution/Confirmed bookings <see cref="BookingSeeder"/> left behind into
/// workfiles at different points of the Open -&gt; ManagerReview -&gt; AccountingReview -&gt; Closed
/// lifecycle, plus one standalone Reefer file (which is never booked against a carrier - see
/// Workfile.Rules). Closes the gap noted in feature-map.md: every workfile status used to require a
/// steward to click through the UI by hand before it existed anywhere in a dev database.
/// </summary>
internal class WorkfileSeeder(ILogger<WorkfileSeeder> logger, IWorkfileReferenceGenerator referenceGenerator)
    : SeederBase(logger)
{
    public override string Name => nameof(WorkfileSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override int Order => 235;
    public override IReadOnlyList<string> DependsOn => [nameof(BookingSeeder), nameof(BusinessPartnerSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return await context.TenantUnitOfWork.Repository<Workfile>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var bookings = context.CreatedBookings;
        var partners = context.CreatedBusinessPartners;
        if (bookings is null || partners is null)
        {
            LogUpstreamSkipped();
            return;
        }

        LogStarting();

        var shippingLine = partners.FirstOrDefault(p => p.Code == "MAERSK");
        var forwarder = partners.FirstOrDefault(p => p.Code == "SKYFWD");
        var broker = partners.FirstOrDefault(p => p.Code == "CBROKER");
        var trucker = partners.FirstOrDefault(p => p.Code == "ABCTRK");

        var repo = context.TenantUnitOfWork.Repository<Workfile>();
        var created = new List<Workfile>();
        var executingBookings = bookings.Where(b => b.Status == BookingStatus.InProgress).ToArray();
        var confirmedBookings = bookings.Where(b => b.Status == BookingStatus.Confirmed).ToArray();

        // #1: first import booking, carried to AccountingReview - forwarded but not yet closed.
        if (executingBookings.Length > 0)
        {
            var booking = executingBookings[0];
            var reference = await referenceGenerator.NextAsync(WorkfileSeries.ForRegular(FreightMode.Import), cancellationToken);
            var workfile = WorkfileFactory.Create(WorkfileType.Regular, FreightMode.Import, null,
                reference, booking.CustomerId, booking.Id, BuildImportHeader(shippingLine, forwarder, broker));
            workfile.SubmitForApproval();
            workfile.MarkForwardedToAccounting();
            await repo.AddAsync(workfile, cancellationToken);
            created.Add(workfile);
        }

        // #2: second import booking, carried all the way to Closed.
        if (executingBookings.Length > 1)
        {
            var booking = executingBookings[1];
            var reference = await referenceGenerator.NextAsync(WorkfileSeries.ForRegular(FreightMode.Import), cancellationToken);
            var workfile = WorkfileFactory.Create(WorkfileType.Regular, FreightMode.Import, null,
                reference, booking.CustomerId, booking.Id, BuildImportHeader(shippingLine, forwarder, broker));
            workfile.SubmitForApproval();
            workfile.MarkForwardedToAccounting();
            workfile.Close("Seed Data");
            await repo.AddAsync(workfile, cancellationToken);
            created.Add(workfile);
        }

        // #3: export booking, left in ManagerReview - awaiting a manager's sign-off.
        if (confirmedBookings.Length > 0)
        {
            var booking = confirmedBookings[0];
            var reference = await referenceGenerator.NextAsync(WorkfileSeries.ForRegular(FreightMode.Export), cancellationToken);
            var header = new WorkfileHeaderData
            {
                BillOfLadingNumber = "MAEU-HBL-778812",
                BillIndicator = BillIndicator.Hbl,
                VesselName = "MV Cebu Feeder",
                VoyageNumber = "0212W",
                Etd = DateTime.UtcNow.Date.AddDays(2),
                Eta = DateTime.UtcNow.Date.AddDays(18),
                Origin = "PHMNL",
                Destination = "USLAX",
                Commodity = "General Merchandise",
                ShippingLineId = shippingLine?.Id,
                TruckerId = trucker?.Id,
                CreditTermDays = 30,
                CreditType = CreditType.Charge,
                ExchangeRate = 56.5m,
                ReportCategory = ReportCategory.Psb
            };
            var workfile = WorkfileFactory.Create(WorkfileType.Regular, FreightMode.Export, null,
                reference, booking.CustomerId, booking.Id, header);
            workfile.SubmitForApproval();
            await repo.AddAsync(workfile, cancellationToken);
            created.Add(workfile);
        }

        // #4: standalone Reefer file - never booked against a carrier, left Open for ops to work.
        var reeferCustomerId = bookings.Count > 0 ? bookings[0].CustomerId : (Guid?)null;
        if (reeferCustomerId.HasValue)
        {
            var reeferReference = await referenceGenerator.NextAsync(
                WorkfileSeries.For(WorkfileType.Reefer, null, null), cancellationToken);
            var header = new WorkfileHeaderData
            {
                Commodity = "Frozen Fish",
                Origin = "Manila Cold Storage",
                Destination = "Davao Distribution Center",
                TruckerId = trucker?.Id,
                CreditTermDays = 15,
                CreditType = CreditType.Cash,
                ReportCategory = ReportCategory.NotPsb
            };
            var reeferWorkfile = WorkfileFactory.Create(
                WorkfileType.Reefer, null, null, reeferReference, reeferCustomerId.Value, null, header);
            await repo.AddAsync(reeferWorkfile, cancellationToken);
            created.Add(reeferWorkfile);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        LogCompleted(created.Count);
    }

    private static WorkfileHeaderData BuildImportHeader(BusinessPartner? shippingLine, BusinessPartner? forwarder, BusinessPartner? broker) => new()
    {
        BillOfLadingNumber = $"MAEU-MBL-{Random.Shared.Next(100000, 999999)}",
        BillIndicator = BillIndicator.Mbl,
        VesselName = "MV Skybest Pioneer",
        VoyageNumber = "0125E",
        Etd = DateTime.UtcNow.Date.AddDays(-10),
        Eta = DateTime.UtcNow.Date.AddDays(2),
        Origin = "CNSHA",
        Destination = "PHMNL",
        Commodity = "Consumer Electronics",
        ShippingLineId = shippingLine?.Id,
        ForwarderId = forwarder?.Id,
        BrokerId = broker?.Id,
        CreditTermDays = 30,
        CreditType = CreditType.Charge,
        ExchangeRate = 56.5m,
        ReportCategory = ReportCategory.Psb
    };
}
