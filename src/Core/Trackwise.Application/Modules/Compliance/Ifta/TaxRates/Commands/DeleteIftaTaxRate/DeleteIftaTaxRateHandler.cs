using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Compliance.Ifta.TaxRates.Commands;

internal sealed class DeleteIftaTaxRateHandler(IMasterUnitOfWork masterUow)
    : DeleteMasterEntityHandler<DeleteIftaTaxRateCommand, IftaTaxRate>(masterUow);
