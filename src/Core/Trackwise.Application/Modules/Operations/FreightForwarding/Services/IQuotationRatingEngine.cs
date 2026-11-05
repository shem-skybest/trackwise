using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

public interface IQuotationRatingEngine
{
    Task AutoPriceAsync(Quotation quotation, Guid? customerContractId, CancellationToken ct = default);
}
