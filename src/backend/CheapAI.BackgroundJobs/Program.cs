using CheapAI.Application;
using CheapAI.Application.Common.Abstractions;
using CheapAI.BackgroundJobs;
using CheapAI.Infrastructure;
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
builder.Services.AddHostedService<RecurringJobRegistrationHostedService>();

builder.Services.AddSerilog(config =>
{
    config
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

var host = builder.Build();
host.Run();
