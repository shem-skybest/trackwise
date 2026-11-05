using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RemoveQuotationLine;

public record RemoveQuotationLineCommand(Guid QuotationId, Guid LineId) : ICommand<Result>;
