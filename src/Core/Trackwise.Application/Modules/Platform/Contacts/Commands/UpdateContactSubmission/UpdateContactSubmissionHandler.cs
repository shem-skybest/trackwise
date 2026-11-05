using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Platform.Contacts.Commands;

internal sealed class UpdateContactSubmissionHandler(
    IMasterUnitOfWork masterUow,
    ILogger<UpdateContactSubmissionHandler> logger) : IAppRequestHandler<UpdateContactSubmissionCommand, Result>
{
    public async Task<Result> Handle(UpdateContactSubmissionCommand req, CancellationToken ct)
    {
        var submission = await masterUow.Repository<ContactSubmission>().GetByIdAsync(req.Id, ct);

        if (submission is null)
        {
            return Result.Fail($"Contact submission with ID '{req.Id}' not found");
        }

        submission.Status = req.Status;
        submission.Notes = req.Notes;
        submission.UpdatedAt = DateTime.UtcNow;

        masterUow.Repository<ContactSubmission>().Update(submission);
        await masterUow.SaveChangesAsync(ct);

        logger.LogInformation("Updated contact submission {Id} to status {Status}", req.Id, submission.Status);
        return Result.Ok();
    }
}
