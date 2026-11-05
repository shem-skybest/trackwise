using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.AcceptQuotation;

public class AcceptQuotationCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
