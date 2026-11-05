using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Queries;

public class GetExpenseByIdQuery : IQuery<Result<ExpenseDto>>, IHaveId
{
    public Guid Id { get; set; }
}
