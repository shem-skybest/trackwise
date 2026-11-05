using Trackwise.IdentityServer;
using Trackwise.HostDefaults;

TrackwiseHost.Run(args, builder => builder
    .ConfigureServices()
    .ConfigurePipeline());
