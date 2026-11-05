using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds business partners - the module's first maker-checker entity. Most rows go straight to
/// <c>Approved</c>/active so Workfile seeding has counterparties to name in every slot; one is left
/// in <c>Pending</c> so the approver queue is never empty on a fresh dev database (see
/// feature-map.md's Booking/Workfile gap notes).
/// <para>
/// A tenant with a real <c>BusinessPartners</c> seed section gets those instead, seeded per record
/// (keyed on name) and approved - they're counterparties the company already deals with. They're
/// kept out of the generated demo workfiles, which only ever look for the placeholder codes.
/// </para>
/// </summary>
internal class BusinessPartnerSeeder(ILogger<BusinessPartnerSeeder> log) : SeederBase(log)
{
    public override string Name => nameof(BusinessPartnerSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 220;
    public override IReadOnlyList<string> DependsOn => [nameof(BranchSeeder), nameof(CustomerSeeder)];

    protected override async Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        if (context.GetSeedBusinessPartners().Length > 0)
        {
            return false;
        }

        return await context.TenantUnitOfWork.Repository<BusinessPartner>().CountAsync(ct: cancellationToken) > 0;
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        if (context.GetSeedBusinessPartners() is { Length: > 0 } realPartners)
        {
            await SeedRealPartnersAsync(context, realPartners, cancellationToken);
            return;
        }

        var branches = context.CreatedBranches;
        if (branches is null)
        {
            LogUpstreamSkipped();
            return;
        }

        if (branches.Count == 0)
        {
            context.CreatedBusinessPartners = [];
            LogCompleted(0);
            return;
        }

        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<BusinessPartner>();
        var seeds = new (string Code, string Name, BusinessPartnerRole[] Roles, string? TaxId, MasterDataApprovalState State)[]
        {
            ("MAERSK", "Maersk Line Philippines Inc.", [BusinessPartnerRole.ShippingLine], "123456789000", MasterDataApprovalState.Approved),
            ("ICTSI", "ICTSI Depot Services", [BusinessPartnerRole.Depot], null, MasterDataApprovalState.Approved),
            ("SKYFWD", "Skybest Forwarding Agents Inc.", [BusinessPartnerRole.Forwarder, BusinessPartnerRole.Agent], "987654321000", MasterDataApprovalState.Approved),
            ("ABCTRK", "ABC Trucking Services", [BusinessPartnerRole.Trucker], null, MasterDataApprovalState.Approved),
            ("CBROKER", "Cortez Customs Brokerage", [BusinessPartnerRole.Broker], "456789123000", MasterDataApprovalState.Approved),
            ("NEWCOLD", "NewCold Logistics Co-Loader", [BusinessPartnerRole.CoLoader], null, MasterDataApprovalState.Pending)
        };

        var created = new List<BusinessPartner>();
        foreach (var seed in seeds)
        {
            var partner = new BusinessPartner
            {
                Code = seed.Code,
                Name = seed.Name,
                Roles = [.. seed.Roles],
                TaxId = seed.TaxId,
                ApprovalState = seed.State,
                IsActive = seed.State is MasterDataApprovalState.Approved
            };
            await repo.AddAsync(partner, cancellationToken);
            created.Add(partner);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedBusinessPartners = created;
        LogCompleted(created.Count);
    }

    private async Task SeedRealPartnersAsync(
        SeederContext context, BusinessPartnerData[] partners, CancellationToken cancellationToken)
    {
        LogStarting();

        var repo = context.TenantUnitOfWork.Repository<BusinessPartner>();
        var codes = context.ServiceProvider.GetRequiredService<IMasterDataCodeGenerator>();
        var existingByName = (await repo.GetListAsync(ct: cancellationToken))
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var customersByName = (await context.TenantUnitOfWork.Repository<Customer>().GetListAsync(ct: cancellationToken))
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var seed in partners)
        {
            // A partner seeded before its customer existed gets the link on a later run.
            if (existingByName.TryGetValue(seed.Name, out var existing))
            {
                if (existing.CustomerId is null
                    && seed.CustomerName is { } linkName
                    && customersByName.TryGetValue(linkName, out var linkCustomer))
                {
                    existing.CustomerId = linkCustomer.Id;
                }

                continue;
            }

            var roles = new List<BusinessPartnerRole>();
            foreach (var role in seed.Roles)
            {
                if (Enum.TryParse<BusinessPartnerRole>(role, out var parsed))
                {
                    roles.Add(parsed);
                }
                else
                {
                    logger.LogWarning("Unknown partner role '{Role}' for {Name} - ignored", role, seed.Name);
                }
            }

            var partner = new BusinessPartner
            {
                Code = await codes.NextAsync(nameof(BusinessPartner), "BP", ct: cancellationToken),
                Name = seed.Name,
                Roles = roles,
                Notes = DescribeSource(seed),
                ApprovalState = MasterDataApprovalState.Approved,
                IsActive = true
            };

            if (seed.CustomerName is { } customerName && customersByName.TryGetValue(customerName, out var customer))
            {
                partner.CustomerId = customer.Id;
            }

            await repo.AddAsync(partner, cancellationToken);
            existingByName[seed.Name] = partner;
            created++;
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedBusinessPartners = [];
        LogCompleted(created);
    }

    private static string DescribeSource(BusinessPartnerData seed)
    {
        var source = seed.Aliases.Length > 0
            ? $"From Monday.com operation boards, recorded as: {string.Join(", ", seed.Aliases)}."
            : "From Monday.com operation boards.";
        return seed.Note is null ? source : $"{source} {seed.Note}";
    }
}
