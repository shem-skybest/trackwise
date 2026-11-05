using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

public class DeleteExpenseCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
