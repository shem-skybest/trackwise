using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Financial.Payroll.Commands;

public class SetupEmployeePayoutCommand : ICommand
{
    public Guid EmployeeId { get; set; }
}
