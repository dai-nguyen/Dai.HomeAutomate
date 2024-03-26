using Dai.HomeAutomate.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/log_.txt", rollingInterval: RollingInterval.Day, shared: true)
            .CreateLogger();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddLogging(c => c.AddSerilog());
builder.Services.AddQuartz(options =>
{
    options.UseInMemoryStore();

    // 0 30 7 * * ? : starts at 7:30 every day

    options.ScheduleJob<TurtleTankJob>(trigger =>
        trigger.WithIdentity("Turtle Tank Job")        
        .UsingJobData("on", true)
        .WithCronSchedule("0 0 7-18 ? * * *", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"))));
});
builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;    
});

using IHost host = builder.Build();

await host.RunAsync();