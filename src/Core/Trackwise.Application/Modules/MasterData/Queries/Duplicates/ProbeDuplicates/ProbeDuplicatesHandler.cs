using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class ProbeDuplicatesHandler(
    IDuplicateProbe probe,
    IMasterDataPolicyProvider policies)
    : IAppRequestHandler<ProbeDuplicatesQuery, Result<DuplicateProbeDto>>
{
    public async Task<Result<DuplicateProbeDto>> Handle(ProbeDuplicatesQuery req, CancellationToken ct)
    {
        var entityType = req.EntityType ?? "";
        var policy = policies.Find(entityType);

        if (policy is null)
        {
            return Result<DuplicateProbeDto>.Fail($"'{entityType}' is not a master-data entity.");
        }

        var result = await probe.ProbeAsync(
            new DuplicateProbeRequest(entityType, req.Fields, req.ExcludeId), ct);

        return Result<DuplicateProbeDto>.Ok(new DuplicateProbeDto
        {
            EntityType = entityType,
            HardThreshold = result.HardThreshold,
            SoftThreshold = result.SoftThreshold,
            IsBlocked = result.IsBlocked,
            Candidates = result.Candidates
                .Select(c => new DuplicateCandidateDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Score = c.Score,
                    MatchedOn = c.MatchedOn.ToList(),
                    State = c.State
                })
                .ToList()
        });
    }
}
