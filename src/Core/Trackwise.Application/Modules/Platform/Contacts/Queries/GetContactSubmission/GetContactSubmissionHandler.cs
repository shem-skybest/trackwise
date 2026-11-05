using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Contacts.Queries;

internal sealed class GetContactSubmissionHandler(
    IMasterUnitOfWork masterUow) : IAppRequestHandler<GetContactSubmissionQuery, Result<ContactSubmissionDto>>
{
    public async Task<Result<ContactSubmissionDto>> Handle(GetContactSubmissionQuery req, CancellationToken ct)
    {
        var submission = await masterUow.Repository<ContactSubmission>().GetByIdAsync(req.Id, ct);

        if (submission is null)
        {
            return Result<ContactSubmissionDto>.Fail($"Contact submission with ID '{req.Id}' not found");
        }

        return Result<ContactSubmissionDto>.Ok(submission.ToDto());
    }
}
