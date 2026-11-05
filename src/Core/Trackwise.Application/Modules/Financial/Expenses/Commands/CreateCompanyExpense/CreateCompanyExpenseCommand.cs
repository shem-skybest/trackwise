using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

public class CreateCompanyExpenseCommand : ICommand<Result<Guid>>, IExpenseFields
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? VendorName { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? ReceiptBlobPath { get; set; }
    public string? Notes { get; set; }
    public CompanyExpenseCategory Category { get; set; }
}
