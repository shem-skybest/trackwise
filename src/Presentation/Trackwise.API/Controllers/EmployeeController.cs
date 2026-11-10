using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trackwise.Application.Modules.IdentityAccess.Employees.Commands;
using Trackwise.Application.Modules.IdentityAccess.Employees.Queries;
using Trackwise.Application.Modules.Financial.Payroll.Commands;
using Trackwise.Application.Modules.Financial.Payroll.Queries;
using Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

namespace Trackwise.API.Controllers;

/// <summary>
/// Self-service and dispatch/report reads of an employee's record, plus driver-license and payout
/// actions that are not master-data stewardship. Creating, editing, deleting and merging the
/// employee record itself now goes through <c>/master-data/employees</c>
/// (<see cref="Trackwise.API.Controllers.MasterData.EmployeeController"/>) - a second write path here
/// would bypass the duplicate probe, the dependency probe and the live-login guard, and one
/// bypassable guarantee is no guarantee.
/// </summary>
[ApiController]
[Route("employees")]
[Produces("application/json")]
public class EmployeeController(IMediator mediator) : ControllerBase
{
    [HttpGet("{userId:guid}", Name = "GetEmployeeById")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Employee.View)]
    public async Task<IActionResult> GetById(Guid userId)
    {
        var result = await mediator.Send(new GetEmployeeByIdQuery { UserId = userId });
        return result.IsSuccess ? Ok(result.Value) : NotFound(ErrorResponse.FromResult(result));
    }

    [HttpGet(Name = "GetEmployees")]
    [ProducesResponseType(typeof(PagedResponse<EmployeeDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Employee.View)]
    public async Task<IActionResult> GetList([FromQuery] GetEmployeesQuery query)
    {
        var result = await mediator.Send(query);
        return Ok(PagedResponse<EmployeeDto>.FromPagedResult(result, query.Page, query.PageSize));
    }

    [HttpPut("{userId:guid}/password", Name = "SetEmployeePassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> SetPassword(Guid userId, [FromBody] SetEmployeePasswordCommand request)
    {
        request.UserId = userId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    /// Moves the person to another role. Separate from the master-data edit on purpose: assigning
    /// access is HR's call (<c>Employee.Manage</c>), editing reference data is a steward's.
    /// </summary>
    [HttpPut("{userId:guid}/role", Name = "AssignEmployeeRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignEmployeeRoleCommand request)
    {
        request.EmployeeId = userId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    #region Driver licenses

    [HttpGet("{userId:guid}/licenses", Name = "GetDriverLicenses")]
    [ProducesResponseType(typeof(IList<DriverLicenseDto>), StatusCodes.Status200OK)]
    [Authorize(Policy = Permission.Employee.View)]
    public async Task<IActionResult> GetDriverLicenses(Guid userId, [FromQuery] bool includeRevoked = false)
    {
        var result = await mediator.Send(new GetDriverLicensesQuery
        {
            EmployeeId = userId,
            IncludeRevoked = includeRevoked
        });
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPost("{userId:guid}/licenses", Name = "CreateDriverLicense")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> CreateDriverLicense(
        Guid userId, [FromBody] CreateDriverLicenseCommand request)
    {
        request.EmployeeId = userId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpPut("{userId:guid}/licenses/{licenseId:guid}", Name = "UpdateDriverLicense")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> UpdateDriverLicense(
        Guid userId, Guid licenseId, [FromBody] UpdateDriverLicenseCommand request)
    {
        request.LicenseId = licenseId;
        var result = await mediator.Send(request);
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    [HttpDelete("{userId:guid}/licenses/{licenseId:guid}", Name = "DeleteDriverLicense")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> DeleteDriverLicense(Guid userId, Guid licenseId)
    {
        var result = await mediator.Send(new DeleteDriverLicenseCommand { LicenseId = licenseId });
        return result.IsSuccess ? NoContent() : NotFound(ErrorResponse.FromResult(result));
    }

    #endregion

    #region Payout

    /// <summary>
    ///     Set up a Stripe Connect Express account for employee payroll payouts.
    /// </summary>
    [HttpPost("{userId:guid}/payout/setup", Name = "SetupEmployeePayout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> SetupEmployeePayout(Guid userId)
    {
        var result = await mediator.Send(new SetupEmployeePayoutCommand { EmployeeId = userId });
        return result.IsSuccess ? NoContent() : BadRequest(ErrorResponse.FromResult(result));
    }

    /// <summary>
    ///     Get the Stripe onboarding link for an employee to complete their payout account setup.
    /// </summary>
    [HttpGet("{userId:guid}/payout/onboarding-link", Name = "GetEmployeePayoutOnboardingLink")]
    [ProducesResponseType(typeof(EmployeePayoutOnboardingLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Permission.Employee.Manage)]
    public async Task<IActionResult> GetEmployeePayoutOnboardingLink(
        Guid userId, [FromQuery] string returnUrl, [FromQuery] string refreshUrl)
    {
        var result = await mediator.Send(new GetEmployeePayoutOnboardingLinkQuery
        {
            EmployeeId = userId,
            ReturnUrl = returnUrl,
            RefreshUrl = refreshUrl
        });
        return result.IsSuccess ? Ok(result.Value) : BadRequest(ErrorResponse.FromResult(result));
    }

    #endregion
}
