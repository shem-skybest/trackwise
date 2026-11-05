// ─────────────────────────────────────────────
// TariffController.cs — corrected
// ─────────────────────────────────────────────
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.AddTariffRate;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.CreateTariff;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.DeactivateTariff;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.RemoveTariffRate;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.SupersedeTariff;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.UpdateTariff;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;

namespace Trackwise.API.Controllers;

[ApiController]
[Route("tariffs")]
[Produces("application/json")]
public class TariffController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetTariffById")]
    [ProducesResponseType(typeof(TariffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTariffByIdQuery { Id = id }, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetTariffs")]
    [ProducesResponseType(typeof(PagedResponse<TariffDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.View)]
    public async Task<IActionResult> GetList([FromQuery] GetTariffsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return result.IsSuccess
            ? Ok(PagedResponse<TariffDto>.FromPagedResult(result, query.Page, query.PageSize))
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost(Name = "CreateTariff")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateTariffCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetTariffById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{id:guid}", Name = "UpdateTariff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTariffCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/rates", Name = "AddTariffRate")]
    [ProducesResponseType(typeof(TariffRateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> AddRate(Guid id, [FromBody] AddTariffRateCommand request, CancellationToken ct)
    {
        request.TariffId = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetTariffById", new { id }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("rates/{rateId:guid}", Name = "RemoveTariffRate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> RemoveRate(
        Guid rateId,
        CancellationToken ct)
    {
        var request = new RemoveTariffRateCommand(rateId);

        var result = await mediator.Send(request, ct);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/deactivate", Name = "DeactivateTariff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, [FromBody] DeactivateTariffCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{id:guid}/supersede", Name = "SupersedeTariff")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Authorize(Policy = Permission.Tariff.Manage)]
    public async Task<IActionResult> Supersede(Guid id, [FromBody] SupersedeTariffCommand request, CancellationToken ct)
    {
        request.Id = id;
        var result = await mediator.Send(request, ct);
        return result.IsSuccess
            ? CreatedAtRoute("GetTariffById", new { id = result.Value }, result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }
}
