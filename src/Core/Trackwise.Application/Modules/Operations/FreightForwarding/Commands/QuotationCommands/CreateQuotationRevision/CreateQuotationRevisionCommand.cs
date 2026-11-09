using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotationRevision;

public sealed record CreateQuotationRevisionCommand(Guid Id) : ICommand<Result<Guid>>;
