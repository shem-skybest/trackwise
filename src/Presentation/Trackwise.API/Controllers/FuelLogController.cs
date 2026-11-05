using Trackwise.Application.Modules.Financial.FuelLog.Commands;
using Trackwise.Application.Modules.Financial.FuelLog.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers;

/// <summary>
/// The fuel log: fill-ups are truck expenses in the fuel category, so they sit under
/// <c>/expenses</c> and are verified, flagged, deleted and given receipts through its endpoints.
/// </summary>
[ApiController]
[Route("expenses/fuel")]
[Produces("application/json")]
public class FuelLogController(IMediator mediator) : ControllerBase
{
    [HttpGet(Name = "GetFuelFillUps")]
    [ProducesResponseType(typeof(PagedResponse<FuelFillUpDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.View)]
    public async Task<IActionResult> GetList([FromQuery] GetFuelFillUpsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<FuelFillUpDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("summary", Name = "GetFuelSummary")]
    [ProducesResponseType(typeof(FuelSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.View)]
    public async Task<IActionResult> GetSummary([FromQuery] GetFuelSummaryQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "RecordFuelFillUp")]
    [ProducesResponseType(typeof(FuelFillUpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> Record([FromBody] RecordFuelFillUpCommand request, CancellationToken ct) =>
        FromResult(await mediator.Send(request, ct));

    [HttpPut("{id:guid}", Name = "UpdateFuelFillUp")]
    [ProducesResponseType(typeof(FuelFillUpDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Expense.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFuelFillUpCommand request, CancellationToken ct)
    {
        request.Id = id;
        return FromResult(await mediator.Send(request, ct));
    }

    private IActionResult FromResult(Result<FuelFillUpDto> result) =>
        result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
}
