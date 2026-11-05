using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Platform.Contacts.Commands;

internal sealed class DeleteContactSubmissionHandler(IMasterUnitOfWork masterUow)
    : DeleteMasterEntityHandler<DeleteContactSubmissionCommand, ContactSubmission>(masterUow);
