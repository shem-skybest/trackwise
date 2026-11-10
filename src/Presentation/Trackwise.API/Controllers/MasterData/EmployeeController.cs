using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Application.Modules.MasterData.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// Stewardship of employees. Writes the same records <c>/employees</c> reads, which keeps that route
/// read-only: a second write path would bypass the duplicate probe, the dependency probe and the
/// live-login guard, and one bypassable guarantee is no guarantee.
/// </summary>
[ApiController]
[Route("master-data/employees")]
[Produces("application/json")]
public class EmployeeController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetEmployeeMasterDataById")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetEmployeeByIdQuery { Id = id }, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetEmployeesMasterData")]
    [ProducesResponseType(typeof(PagedResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetList([FromQuery] GetEmployeesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<EmployeeDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("{id:guid}/history", Name = "GetEmployeeHistory")]
    [ProducesResponseType(typeof(PagedResponse<MasterDataChangeLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.View)]
    public async Task<IActionResult> GetHistory(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new GetMasterDataChangeLogQuery
        {
            EntityType = nameof(Trackwise.Domain.Entities.Employee),
            EntityId = id,
            Page = page,
            PageSize = pageSize
        };

        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<MasterDataChangeLogDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateEmployeeMasterData")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);

        if (result.IsSuccess)
        {
            return CreatedAtRoute("GetEmployeeMasterDataById", new { id = result.Value!.Id }, result.Value);
        }

        return Failure(result);
    }

    private IActionResult Failure(Trackwise.Shared.Models.IResult result) =>
        MasterDataFailure.From(this, result);

    [HttpPut("{id:guid}", Name = "UpdateEmployeeMasterData")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    // HR keeps employee records without holding rights over the rest of master data.
    [Authorize(Policy = $"{Permission.MasterData.Manage}|{Permission.Employee.Manage}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    [HttpDelete("{id:guid}", Name = "DeleteEmployeeMasterData")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] bool confirmedDespiteDependents = false,
        [FromQuery] string? dependencyReason = null,
        CancellationToken ct = default)
    {
        var command = new DeleteEmployeeCommand
        {
            Id = id,
            ConfirmedDespiteDependents = confirmedDespiteDependents,
            DependencyReason = dependencyReason
        };

        var result = await mediator.Send(command, ct);
        return result.IsSuccess ? NoContent() : Failure(result);
    }

    /// <summary>
    /// Folds one employee into another. The survivor keeps its code and inherits every reference the
    /// loser held.
    /// </summary>
    [HttpPost("{id:guid}/merge", Name = "MergeEmployees")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Merge(
        Guid id, [FromBody] MergeEmployeesCommand request, CancellationToken ct)
    {
        request.SurvivorId = id;

        var result = await mediator.Send(request, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/restore", Name = "RestoreEmployee")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.MasterData.Manage)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new RestoreEmployeeCommand { Id = id }, ct);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
