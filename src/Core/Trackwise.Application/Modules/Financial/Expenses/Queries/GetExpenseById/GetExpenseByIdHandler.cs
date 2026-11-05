using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Queries;

internal sealed class GetExpenseByIdHandler(ITenantUnitOfWork tenantUow)
    : GetTenantEntityByIdHandler<GetExpenseByIdQuery, Expense, ExpenseDto>(tenantUow)
{
    protected override ExpenseDto MapToDto(Expense entity) => entity.ToDto();
}
