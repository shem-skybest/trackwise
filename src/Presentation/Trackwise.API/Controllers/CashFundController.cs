using Trackwise.Application.Modules.Financial.CashFunds.Commands;
using Trackwise.Application.Modules.Financial.CashFunds.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>
/// Operational funds and their liquidation. Guarded by the expense permissions: Ops records and
/// forwards what was spent (<c>Expense.Manage</c>), the office checks it and closes the fund
/// (<c>Expense.Approve</c>).
/// </summary>
[ApiController]
[Route("cash-funds")]
[Produces("application/json")]
public class CashFundController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetCashFunds")]
    [ProducesResponseType(typeof(PagedResponse<CashFundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.View)]
    public async Task<IActionResult> GetList([FromQuery] GetCashFundsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<CashFundDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}", Name = "GetCashFundById")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Expense.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCashFundByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateCashFund")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateCashFundCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetCashFundById", new { id = result.Value!.Id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateCashFund")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCashFundCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpDelete("{id:guid}", Name = "DeleteCashFund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DeleteCashFundCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/expenses", Name = "AddCashFundExpense")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> AddExpense(Guid id, [FromBody] AddCashFundExpenseCommand request, CancellationToken ct)
    {
        request.CashFundId = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpPut("{id:guid}/expenses/{expenseId:guid}", Name = "UpdateCashFundExpense")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> UpdateExpense(
        Guid id, Guid expenseId, [FromBody] UpdateCashFundExpenseCommand request, CancellationToken ct)
    {
        request.CashFundId = id;
        request.ExpenseId = expenseId;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpDelete("{id:guid}/expenses/{expenseId:guid}", Name = "DeleteCashFundExpense")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> DeleteExpense(Guid id, Guid expenseId, CancellationToken ct)
    {
        var command = new DeleteCashFundExpenseCommand { CashFundId = id, ExpenseId = expenseId };
        return FromResult(await mediator.Send(command, ct));
    }

    [HttpPost("{id:guid}/forward", Name = "ForwardCashFundExpenses")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> Forward(Guid id, [FromBody] ForwardCashFundExpensesCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpPost("{id:guid}/review", Name = "ReviewCashFundExpenses")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Approve)]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewCashFundExpensesCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    [HttpPost("{id:guid}/liquidate", Name = "LiquidateCashFund")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Approve)]
    public async Task<IActionResult> Liquidate(Guid id, CancellationToken ct) =>
        FromResult(await mediator.Send(new LiquidateCashFundCommand { Id = id }, ct));

    [HttpPost("{id:guid}/reopen", Name = "ReopenCashFund")]
    [ProducesResponseType(typeof(CashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Approve)]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct) =>
        FromResult(await mediator.Send(new ReopenCashFundCommand { Id = id }, ct));

    private IActionResult FromResult(Result<CashFundDto> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
}
