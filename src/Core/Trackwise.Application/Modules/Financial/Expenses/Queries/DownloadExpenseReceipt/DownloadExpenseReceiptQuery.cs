using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Queries;

public class DownloadExpenseReceiptQuery : IQuery<Result<DocumentDownloadDto>>
{
    public Guid ExpenseId { get; set; }
    public Guid RequestedById { get; set; }
}
