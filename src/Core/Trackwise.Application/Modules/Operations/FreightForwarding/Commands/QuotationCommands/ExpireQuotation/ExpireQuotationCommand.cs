using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ExpireQuotation;

public class ExpireQuotationCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
