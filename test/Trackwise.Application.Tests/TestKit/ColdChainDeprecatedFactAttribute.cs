using Xunit;

namespace Trackwise.Application.Tests.TestKit;

/// <summary>
/// Skips tests for the deprecated Cold Chain (rate-agreement booking) path.
/// Remove this attribute, or switch back to [Fact], to re-enable them.
/// </summary>
public sealed class ColdChainDeprecatedFactAttribute : FactAttribute
{
    public ColdChainDeprecatedFactAttribute()
    {
        Skip = "Cold Chain (rate-agreement bookings) is deprecated.";
    }
}
