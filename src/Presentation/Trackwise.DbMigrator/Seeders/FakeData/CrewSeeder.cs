using System.Globalization;
using System.Text;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.DbMigrator.Abstractions;
using Trackwise.DbMigrator.Models;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Roles;

namespace Trackwise.DbMigrator.Seeders.FakeData;

/// <summary>
/// Seeds the tenant's real drivers and helpers (the <c>Crew</c> seed section): drivers as users +
/// employees with the driver role, helpers as <see cref="Helper" /> records. Like
/// <see cref="StaffSeeder" />, these are never drafted into generated loads, trips or payroll, and
/// it seeds per record so adding someone to the roster later just works. Someone already seeded gets
/// only a missing employment type and home unit filled in, so edits made since are kept.
/// </summary>
/// <remarks>
/// An employee can only exist as a login user, and drivers have no company email, so each driver
/// gets a non-deliverable placeholder address on <see cref="PlaceholderEmailDomain" />. These are
/// real people and the prod migrator runs this seeder too, so a password is set only in
/// Development; elsewhere the account is passwordless until someone deliberately gives it access.
/// </remarks>
internal class CrewSeeder(ILogger<CrewSeeder> log, IHostEnvironment environment) : SeederBase(log)
{
    private const string DevPassword = "Test12345#";
    private const string PlaceholderEmailDomain = "crew.skybest.local";

    public override string Name => nameof(CrewSeeder);
    public override SeederType Type => SeederType.FakeData;
    public override bool SeedsInProduction => true;
    public override int Order => 116;
    public override IReadOnlyList<string> DependsOn => [nameof(EmployeeSeeder)];

    protected override Task<bool> HasExistingDataAsync(SeederContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(context.GetSeedCrew().Length == 0);
    }

    public override async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        LogStarting();

        var tenant = context.CurrentTenant ?? throw new InvalidOperationException("Current tenant not set");
        var roles = await context.TenantUnitOfWork.Repository<TenantRole>().GetListAsync(ct: cancellationToken);
        var driverRole = roles.First(r => r.Name == TenantRoles.Driver);
        var codes = context.ServiceProvider.GetRequiredService<IMasterDataCodeGenerator>();
        var employeeRepository = context.TenantUnitOfWork.Repository<Employee>();
        var helperRepository = context.TenantUnitOfWork.Repository<Helper>();

        var existingHelpers = (await helperRepository.GetListAsync(ct: cancellationToken))
            .GroupBy(h => h.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var created = 0;

        foreach (var crew in context.GetSeedCrew())
        {
            if (string.Equals(crew.Role, "Driver", StringComparison.OrdinalIgnoreCase))
            {
                var email = PlaceholderEmail(crew);
                var user = await context.UserManager.FindByNameAsync(email)
                           ?? await CreateUserAsync(context, tenant.Id, crew, email);

                if (await employeeRepository.GetByIdAsync(user.Id) is { } existing)
                {
                    existing.EmploymentType ??= EmploymentOf(crew);
                    existing.HomeUnit ??= UnitOf(crew);
                    continue;
                }

                var employee = Employee.CreateEmployeeFromUser(user);
                employee.Code = await codes.NextAsync(nameof(Employee), "EMP", ct: cancellationToken);
                employee.RoleId = driverRole.Id;
                employee.IsActive = crew.IsActive;
                employee.EmploymentType = EmploymentOf(crew);
                employee.HomeUnit = UnitOf(crew);
                await employeeRepository.AddAsync(employee, cancellationToken);
                created++;
            }
            else if (string.Equals(crew.Role, "Helper", StringComparison.OrdinalIgnoreCase))
            {
                var name = FullName(crew);
                if (existingHelpers.TryGetValue(name, out var existing))
                {
                    existing.EmploymentType ??= EmploymentOf(crew);
                    existing.HomeUnit ??= UnitOf(crew);
                    continue;
                }

                var helper = new Helper
                {
                    Code = await codes.NextAsync(nameof(Helper), "HLP", ct: cancellationToken),
                    Name = name,
                    Notes = string.IsNullOrEmpty(crew.LastName) ? "Last name unknown - HR to complete." : null,
                    EmploymentType = EmploymentOf(crew),
                    HomeUnit = UnitOf(crew),
                    IsActive = crew.IsActive
                };
                existingHelpers[name] = helper;
                await helperRepository.AddAsync(helper, cancellationToken);
                created++;
            }
            else
            {
                logger.LogWarning("Unknown crew role '{Role}' for {Name} - skipped", crew.Role, FullName(crew));
            }
        }

        await context.TenantUnitOfWork.SaveChangesAsync(cancellationToken);
        await context.MasterUnitOfWork.SaveChangesAsync(cancellationToken);
        LogCompleted(created);
    }

    private async Task<User> CreateUserAsync(SeederContext context, Guid tenantId, CrewData crew, string email)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            FirstName = crew.FirstName,
            LastName = crew.LastName,
            TenantId = tenantId
        };

        var result = environment.IsDevelopment()
            ? await context.UserManager.CreateAsync(user, DevPassword)
            : await context.UserManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create crew user {email}: {result.Errors.First().Description}");
        }

        return user;
    }

    private static string FullName(CrewData crew) => $"{crew.FirstName} {crew.LastName}".Trim();

    private static EmploymentType EmploymentOf(CrewData crew) =>
        Enum.TryParse<EmploymentType>(crew.EmploymentType, true, out var type) ? type : EmploymentType.Regular;

    private static CrewUnit UnitOf(CrewData crew) =>
        Enum.TryParse<CrewUnit>(crew.Unit, true, out var unit) ? unit : CrewUnit.ColdChain;

    /// <summary>
    /// <c>Mark Anthony Alvero</c> becomes <c>markanthony.alvero</c>: ASCII letters and digits only,
    /// because Identity's allowed user-name characters stop at <c>_.@</c>.
    /// </summary>
    private static string PlaceholderEmail(CrewData crew)
    {
        var parts = new[] { crew.FirstName, crew.LastName }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(Slug);
        return $"{string.Join('.', parts)}@{PlaceholderEmailDomain}";
    }

    private static string Slug(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.ToString();
    }
}
