using CheapAI.Application;
using CheapAI.Application.Common.Abstractions;
using CheapAI.BackgroundJobs;
using CheapAI.Infrastructure;
using Quartz;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<ICurrentAdminAccessor, BackgroundAdminAccessor>();
builder.Services.AddScoped<CheapAiRecurringJobs>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddQuartz(q =>
{
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(options => options.MaxConcurrency = 3);

    q.ScheduleJob<PriceCrawlQuartzJob>(trigger => trigger
        .WithIdentity("cheapai-price-crawl-hourly")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(1).RepeatForever()));

    q.ScheduleJob<AutoTestQuartzJob>(trigger => trigger
        .WithIdentity("cheapai-auto-test-minutely")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithIntervalInMinutes(1).RepeatForever()));

    q.ScheduleJob<RiskRecalculationQuartzJob>(trigger => trigger
        .WithIdentity("cheapai-risk-recalculation-hourly")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(1).RepeatForever()));

    q.ScheduleJob<RankingRebuildQuartzJob>(trigger => trigger
        .WithIdentity("cheapai-ranking-rebuild-hourly")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithIntervalInHours(1).RepeatForever()));
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddSerilog(config =>
{
    config
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var host = builder.Build();
host.Run();
