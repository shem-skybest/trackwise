namespace Trackwise.Shared.Models;

public record SetupIntentDto
{
    public required string ClientSecret { get; init; }
}
