using Microsoft.AspNetCore.Authorization;

namespace Trackwise.API.Authorization;

/// <summary>
/// Met when the user holds any one of <see cref="Permissions"/>. A policy name joins alternatives
/// with <see cref="AnyOfSeparator"/>, e.g. <c>Permission.MasterData.Manage|Permission.Employee.Manage</c>.
/// </summary>
internal class PermissionRequirement(string policyName) : IAuthorizationRequirement
{
    public const char AnyOfSeparator = '|';

    public IReadOnlyList<string> Permissions { get; } =
        policyName.Split(AnyOfSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
