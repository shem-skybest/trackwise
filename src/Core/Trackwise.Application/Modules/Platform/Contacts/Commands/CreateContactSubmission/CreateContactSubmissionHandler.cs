using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Platform.Contacts.Commands;

internal sealed class CreateContactSubmissionHandler(
    IMasterUnitOfWork masterUow,
    ILogger<CreateContactSubmissionHandler> logger) : IAppRequestHandler<CreateContactSubmissionCommand, Result>
{
    public async Task<Result> Handle(CreateContactSubmissionCommand req, CancellationToken ct)
    {
        var submission = new ContactSubmission
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            Phone = req.Phone,
            Subject = req.Subject,
            Message = req.Message,
        };

        await masterUow.Repository<ContactSubmission>().AddAsync(submission, ct);
        await masterUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created contact submission from {Email} with subject {Subject}",
            req.Email,
            req.Subject);

        return Result.Ok();
    }
}
