using Trackwise.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Trackwise.API.Controllers.MasterData;

/// <summary>
/// Maps a failed master-data result to a status code, the same way for every entity.
/// <para>
/// A duplicate, a record something still depends on, or a submission the caller may not decide is a
/// conflict with existing state rather than a malformed request - the distinction is what tells a
/// client to show the "did you mean?", "still in use" or "not yours to approve" dialog instead of a
/// field error. Shared rather than repeated per controller, because the codes are pipeline-wide: an
/// entity whose controller quietly returned 400 for them would break the dialog for that entity
/// alone, and nothing would fail.
/// </para>
/// <para>
/// ⚠️ Self-approval is a <b>409</b> and not a 403 on purpose. 403 says "you lack the permission",
/// which is wrong and unactionable - the caller has the permission, and being told otherwise sends
/// them to an administrator who cannot help. 409 says what is true: this particular record is in a
/// state your own hand put it in.
/// </para>
/// </summary>
internal static class MasterDataFailure
{
    public static IActionResult From(
        ControllerBase controller, Trackwise.Shared.Models.IResult result) =>
        result.ErrorCode is ErrorCodes.DuplicateMasterDataRecord
            or ErrorCodes.MasterDataRecordInUse
            or ErrorCodes.MasterDataSelfApproval
            ? controller.Conflict(ErrorResponse.FromResult(result))
            : controller.BadRequest(ErrorResponse.FromResult(result));
}
