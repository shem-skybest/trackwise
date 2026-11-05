using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.UpdateQuotation;

public sealed record UpdateQuotationCommand(Guid Id, CreateQuotationCommand Data)
    : ICommand<Result<Guid>>;
