using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds customers from fake-dataset.json configuration.
/// </summary>
internal class CustomerSeeder(ILogger<CustomerSeeder> logger) : SeederBase(logger)
{
    public override string Name => nameof(CustomerSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 120;

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();
        var seedKey = context.SeedDataKey ?? throw new InvalidOperationException("Seed data key not set");
        // ph.json holds real client data and is git-ignored, so a fresh checkout (CI) has no section.
        var customers = context.Configuration.GetSection($"{seedKey}:Customers").Get<Customer[]>() ?? [];
        if (customers.Length == 0)
        {
            logger.LogWarning("No '{SeedKey}:Customers' seed data found - no customers seeded", seedKey);
        }

        var customersList = new List<Customer>();
        var customerRepository = context.TenantUnitOfWork.Repository<Customer>();

        foreach (var customer in customers)
        {
            var existingCustomer = await customerRepository.GetAsync(i => i.Name == customer.Name, cancellationToken);

            if (existingCustomer is not null)
            {
                BackfillMissingSeedData(existingCustomer, customer);
                customersList.Add(existingCustomer);
                continue;
            }

            customersList.Add(customer);
            await customerRepository.AddAsync(customer, cancellationToken);
            logger.LogInformation("Created customer '{CustomerName}'", customer.Name);
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        context.CreatedCustomers = customersList;
        LogCompleted(customersList.Count);
    }

    private static void BackfillMissingSeedData(Customer existingCustomer, Customer seedCustomer)
    {
        existingCustomer.Address ??= seedCustomer.Address;
        existingCustomer.Email ??= seedCustomer.Email;
        existingCustomer.Phone ??= seedCustomer.Phone;
        existingCustomer.Notes ??= seedCustomer.Notes;
        existingCustomer.TaxId ??= seedCustomer.TaxId;
        existingCustomer.BillingCycle ??= seedCustomer.BillingCycle;

        if (seedCustomer.IsVatExempt)
        {
            existingCustomer.IsVatExempt = true;
        }
    }
}
