using Hangfire;
using Trackwise.Application.Abstractions.BackgroundJobs;
using Trackwise.Application.Abstractions.Negotiation;

namespace Trackwise.API.Jobs;

public class HangfireNegotiationWakeRunner(IBackgroundJobClient jobClient)
    : IDelayedBackgroundJobRunner<NegotiationWakeRequest>
{
    public void Schedule(NegotiationWakeRequest request, TimeSpan delay)
    {
        jobClient.Schedule<NegotiationWakeJob>(
            job => job.RunAsync(request.TenantId, request.NegotiationId, CancellationToken.None),
            delay);
    }
}
