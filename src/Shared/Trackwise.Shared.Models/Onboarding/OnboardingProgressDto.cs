using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public record OnboardingProgressDto
{
    public OperatingMode OperatingMode { get; init; }
    public List<OnboardingStepDto> Steps { get; init; } = [];
}
