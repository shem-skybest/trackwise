using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// The manager's sign-off: hands the file to Accounting. Once workfile charges exist, marking them
/// forwarded belongs in this handler too - the domain method is already named for that flow.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class ForwardWorkfileToAccountingCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
