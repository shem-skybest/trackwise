using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

public class ApproveExpenseCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public required string ApproverId { get; set; }
}
