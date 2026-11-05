namespace Trackwise.Shared.Models;

public class DispatchTripResultDto
{
    /// <summary>
    /// Non-blocking eligibility notes the dispatcher should see, e.g. a truck with no gross vehicle
    /// weight on file. Anything blocking fails the dispatch instead.
    /// </summary>
    public string[] Warnings { get; set; } = [];
}
