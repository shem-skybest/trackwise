using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// Candidate records that look like the one being entered, with the reason each matched. Advisory:
/// a person decides, the probe never merges or rejects on its own.
/// </summary>
public class DuplicateProbeDto
{
    public required string EntityType { get; set; }

    /// <summary>At or above this score a write is refused unless declared distinct with a reason.</summary>
    public double HardThreshold { get; set; }

    /// <summary>At or above this score candidates are shown as a warning.</summary>
    public double SoftThreshold { get; set; }

    /// <summary>True when at least one candidate is at or above the hard threshold.</summary>
    public bool IsBlocked { get; set; }

    public List<DuplicateCandidateDto> Candidates { get; set; } = [];
}

public class DuplicateCandidateDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary>0-1, where an exact identifier match is 1.</summary>
    public double Score { get; set; }

    /// <summary>Why it matched, e.g. ["name"] or ["code", "name"].</summary>
    public List<string> MatchedOn { get; set; } = [];

    /// <summary>
    /// Included so a steward can see the candidate is inactive or deleted - the most common
    /// duplicate is one somebody deleted last year.
    /// </summary>
    public MasterDataState State { get; set; }
}
