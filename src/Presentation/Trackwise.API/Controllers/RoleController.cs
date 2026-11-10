using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.IdentityAccess.Roles.Commands;
using Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

namespace Trackwise.API.Controllers;

/// <summary>
/// Roles and what each may do. Every write is <c>TenantRole.Manage</c> at the door, then checked
/// again per change by <c>ITenantRoleAccess</c> (no granting what you don't hold, no editing your
/// own role, Owner locked) - holding the policy alone is not enough.
/// </summary>
[ApiController]
[Route("roles")]
[Produces("application/json")]
public class RoleController(IMediator mediator) : ControllerBase
{
    [HttpGet("app", Name = "GetAppRoles")]
    [ProducesResponseType(typeof(PagedResponse<RoleDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.AppRole.View)]
    public async Task<IActionResult> GetAppRoles([FromQuery] GetAppRolesQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<RoleDto>.FromPagedResult(result, query.Page, query.PageSize));
    }

    [HttpGet("tenant", Name = "GetTenantRoles")]
    [ProducesResponseType(typeof(PagedResponse<RoleDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.TenantRole.View)]
    public async Task<IActionResult> GetTenantRoles([FromQuery] GetTenantRolesQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<RoleDto>.FromPagedResult(result, query.Page, query.PageSize));
    }

    [HttpGet("{roleName}/permissions", Name = "GetRolePermissions")]
    [ProducesResponseType(typeof(PermissionDto[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.TenantRole.View)]
    public async Task<IActionResult> GetPermissions(string roleName)
    {
        var result = await mediator.Send(new GetRolePermissionsQuery { RoleName = roleName });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpGet("catalog", Name = "GetPermissionCatalog")]
    [ProducesResponseType(typeof(PermissionGroupDto[]), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.TenantRole.View)]
    public async Task<IActionResult> GetPermissionCatalog()
    {
        var result = await mediator.Send(new GetPermissionCatalogQuery());
        return Ok(result.Value);
    }

    [HttpGet("tenant/history", Name = "GetTenantRoleChangeLog")]
    [ProducesResponseType(typeof(PagedResponse<TenantRoleChangeLogDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.TenantRole.Manage)]
    public async Task<IActionResult> GetChangeLog([FromQuery] GetTenantRoleChangeLogQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<TenantRoleChangeLogDto>.FromPagedResult(result, query.Page, query.PageSize));
    }

    [HttpPost("tenant", Name = "CreateTenantRole")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.TenantRole.Manage)]
    public async Task<IActionResult> CreateTenantRole([FromBody] CreateTenantRoleCommand request)
    {
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("tenant/{roleName}/permissions", Name = "UpdateTenantRolePermissions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.TenantRole.Manage)]
    public async Task<IActionResult> UpdatePermissions(string roleName, [FromBody] UpdateTenantRolePermissionsCommand request)
    {
        request.RoleName = roleName;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("tenant/{roleName}/reset", Name = "ResetTenantRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.TenantRole.Manage)]
    public async Task<IActionResult> ResetRole(string roleName)
    {
        var result = await mediator.Send(new ResetTenantRoleCommand { RoleName = roleName });
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("tenant/{roleName}", Name = "DeleteTenantRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.TenantRole.Manage)]
    public async Task<IActionResult> DeleteRole(string roleName)
    {
        var result = await mediator.Send(new DeleteTenantRoleCommand { RoleName = roleName });
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }
}
