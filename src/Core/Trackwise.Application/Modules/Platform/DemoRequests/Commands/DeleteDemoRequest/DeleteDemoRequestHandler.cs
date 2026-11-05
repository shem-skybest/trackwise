using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Platform.DemoRequests.Commands;

internal sealed class DeleteDemoRequestHandler(IMasterUnitOfWork masterUow)
    : DeleteMasterEntityHandler<DeleteDemoRequestCommand, DemoRequest>(masterUow);
