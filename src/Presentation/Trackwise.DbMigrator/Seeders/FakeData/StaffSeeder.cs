using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the tenant's real office staff (the <c>Staff</c> seed section) as users + employees with
/// their mapped role. Unlike <see cref="EmployeeSeeder" />, these are never drafted into generated
/// loads, trips or payroll, and it seeds per record so adding someone to the roster later just works.
/// </summary>
/// <remarks>
/// These are real people's addresses and the prod migrator runs this seeder too, so a password is
/// set only in Development. Elsewhere the account is created passwordless and the person gets in
/// through a password reset.
/// </remarks>
internal class StaffSeeder(ILogger<StaffSeeder> logger, IHostEnvironment environment) : SeederBase(logger)
{
    private const string DevPassword = "Test12345#";

    public override string Name => nameof(StaffSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 115;
    public override IReadOnlyList<string> DependsOn => [nameof(EmployeeSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedStaff().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var tenant = context.CurrentTenant ?? throw new InvalidOperationException("Current tenant not set");
        var roles = await context.TenantUnitOfWork.Repository<TenantRole>().GetListAsync(ct: cancellationToken);
        var rolesByName = roles.ToDictionary(r => r.Name);
        var employeeRepository = context.TenantUnitOfWork.Repository<Employee>();
        var codes = context.ServiceProvider.GetRequiredService<IMasterDataCodeGenerator>();
        var created = 0;
        var reassigned = 0;
        var handPlaced = (await context.TenantUnitOfWork.Repository<TenantRoleChangeLog>()
                .GetListAsync(l => l.ChangeType == TenantRoleChangeType.EmployeeAssigned && l.EmployeeId != null, cancellationToken))
            .Select(l => l.EmployeeId!.Value)
            .ToHashSet();

        foreach (var staff in context.GetSeedStaff())
        {
            if (!rolesByName.TryGetValue(staff.Role, out var role))
            {
                logger.LogWarning("Unknown staff role '{Role}' for {Email} - skipped", staff.Role, staff.Email);
                continue;
            }

            var user = await context.UserManager.FindByNameAsync(staff.Email)
                       ?? await CreateUserAsync(context, tenant.Id, staff);

            if (await employeeRepository.GetByIdAsync(user.Id) is { } existing)
            {
                // Backfills rosters seeded before employees had a job title; never overwrites one
                // someone has since edited in the app.
                existing.JobTitle ??= staff.Title;

                // The roster's role wins only until someone places the person in the app: an
                // EmployeeAssigned row means HR or an admin chose their role, and that choice stands.
                if (existing.RoleId != role.Id && !handPlaced.Contains(existing.Id))
                {
                    existing.RoleId = role.Id;
                    reassigned++;
                }

                continue;
            }

            var employee = Employee.CreateEmployeeFromUser(user);
            employee.JobTitle = staff.Title;
            employee.Code = await codes.NextAsync(nameof(Employee), "EMP", ct: cancellationToken);
            employee.Address = context.Region?.CompanyAddress;
            employee.RoleId = role.Id;
            if (staff.JoinedDate is { } joined)
            {
                employee.JoinedDate = DateTime.SpecifyKind(joined, DateTimeKind.Utc);
            }

            await employeeRepository.AddAsync(employee, cancellationToken);
            created++;
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        await context.MasterUnitOfWork.SaveChangesAsync(cancellationToken);
        if (reassigned > 0)
        {
            logger.LogInformation("Moved {Count} staff to the role their job title maps to", reassigned);
        }

        LogCompleted(created);
    }

    private async Task<User> CreateUserAsync(SeederContext context, Guid tenantId, StaffData staff)
    {
        var user = new User
        {
            UserName = staff.Email,
            Email = staff.Email,
            FirstName = staff.FirstName,
            LastName = staff.LastName,
            PhoneNumber = staff.PhoneNumber,
            EmailConfirmed = true,
            TenantId = tenantId
        };

        var result = environment.IsDevelopment()
            ? await context.UserManager.CreateAsync(user, DevPassword)
            : await context.UserManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create staff user {staff.Email}: {result.Errors.First().Description}");
        }

        return user;
    }
}
