using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One person's status for one day of the crew plan: a driver (an <see cref="Employee" />) or a
/// <see cref="Helper" />, never both. Monday typed this into the name ("ANDRES ( ABSENT )"); here it
/// is a record, so the pickers can leave out whoever isn't working that day.
/// <para>
/// Kept sparse: a day with no entry means <see cref="CrewDayStatus.Available" />, and setting a plain
/// "available" with no notes removes the entry rather than storing it.
/// </para>
/// </summary>
public class CrewStatusEntry : AuditableEntity, ITenantEntity
{
    private string? notes;

    /// <summary>The day, in the tenant's local calendar - the same day a <see cref="CrewAssignment" /> belongs to.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Set when the person is a driver.</summary>
    public Guid? EmployeeId { get; private set; }

    public virtual Employee? Employee { get; private set; }

    /// <summary>Set when the person is a helper.</summary>
    public Guid? HelperId { get; private set; }

    public virtual Helper? Helper { get; private set; }

    public CrewDayStatus Status { get; set; }

    /// <summary>Why - the leave type, the seminar, which unit they were lent to.</summary>
    public string? Notes
    {
        get => notes;
        set => notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>The employee or helper id - the same ids a <see cref="CrewAssignment" /> holds.</summary>
    public Guid PersonId => EmployeeId ?? HelperId!.Value;

    public static CrewStatusEntry ForDriver(DateOnly date, Guid employeeId) =>
        new() { Date = date, EmployeeId = employeeId };

    public static CrewStatusEntry ForHelper(DateOnly date, Guid helperId) =>
        new() { Date = date, HelperId = helperId };
}
