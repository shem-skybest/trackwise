using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.IssueQuotation;

public class IssueQuotationCommand : ICommand<Result>
{
    public Guid Id { get; set; }

    /// <summary>
    /// Duration before the quote expires. Defaults to 14 days if unspecified.
    /// </summary>
    public int ValidityDays { get; set; } = 14;
}
