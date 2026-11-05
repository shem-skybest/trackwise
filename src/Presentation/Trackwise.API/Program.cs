using Trackwise.API;
using Trackwise.HostDefaults;

DotNetEnv.Env.TraversePath().Load();

TrackwiseHost.Run(args, builder => builder
    .ConfigureServices()
    .ConfigurePipeline()
    .ScheduleJobs());
