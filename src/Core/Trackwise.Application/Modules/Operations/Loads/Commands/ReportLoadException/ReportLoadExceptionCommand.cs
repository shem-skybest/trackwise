using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class ReportLoadExceptionCommand : ICommand<Result>
{
    public Guid LoadId { get; set; }
    public LoadExceptionType Type { get; set; }
    public required string Reason { get; set; }
}
