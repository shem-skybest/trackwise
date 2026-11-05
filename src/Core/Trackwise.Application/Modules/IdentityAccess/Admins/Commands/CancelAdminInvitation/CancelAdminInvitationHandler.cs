using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Commands;

internal sealed class CancelAdminInvitationHandler(IMasterUnitOfWork masterUow)
    : IAppRequestHandler<CancelAdminInvitationCommand, Result>
{
    public async Task<Result> Handle(CancelAdminInvitationCommand req, CancellationToken ct)
    {
        var invitation = await masterUow.Repository<Invitation>()
            .GetAsync(i => i.Id == req.Id && i.Type == InvitationType.AppUser, ct);

        if (invitation is null)
        {
            return Result.Fail("Invitation not found.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result.Fail("Only pending invitations can be cancelled.");
        }

        invitation.Status = InvitationStatus.Cancelled;

        masterUow.Repository<Invitation>().Update(invitation);
        await masterUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
