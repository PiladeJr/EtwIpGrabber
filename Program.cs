using EtwIpGrabber;
using EtwIpGrabber.EtwStructure.EventDispatcher;
using EtwIpGrabber.EtwStructure.MetricsAndHealth;
using EtwIpGrabber.EtwStructure.ProviderConfiguration;
using EtwIpGrabber.EtwStructure.ProviderConfiguration.Abstractions;
using EtwIpGrabber.EtwStructure.RealTimeConsumer;
using EtwIpGrabber.EtwStructure.SessionManager;
using EtwIpGrabber.EtwStructure.SessionManager.Abstraction;
using EtwIpGrabber.EtwStructure.SessionManager.Configuration;
using EtwIpGrabber.EtwStructure.SessionManager.Configuration.Implementation;
using EtwIpGrabber.EtwStructure.SessionManager.Native;
using Microsoft.Extensions.Logging.EventLog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();


// metrics singleton
builder.Services.AddSingleton<EtwMetricsCollector>();

// dispatcher singleton
builder.Services.AddSingleton<BoundedEventRingBuffer>(sp =>
{
    var metrics = sp.GetRequiredService<EtwMetricsCollector>();
    return new BoundedEventRingBuffer(65536, metrics);
});

// ETW core components
builder.Services.AddSingleton<IEtwSessionConfig>(_ =>DefaultEtwSessionConfig.Create());
builder.Services.AddSingleton<EtwSessionPropertiesFactory>();
builder.Services.AddSingleton<IEtwSessionController, EtwSessionController>();
builder.Services.AddSingleton<IEtwProviderConfigurator, TcpIpProviderConfigurator>();
builder.Services.AddSingleton<IRealtimeEtwConsumer, RealtimeEtwConsumer>(sp =>
{
    var dispatcher = sp.GetRequiredService<BoundedEventRingBuffer>();
    var logger = sp.GetRequiredService<ILogger<RealtimeEtwConsumer>>();
    return new RealtimeEtwConsumer(dispatcher, logger);
});

// monitor
builder.Services.AddSingleton<EtwTelemetryMonitor>();

// worker
builder.Services.AddHostedService<Worker>();

builder.Logging.ClearProviders();

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "EtwIpGrabber";
    settings.LogName = "Application";
});

builder.Logging.AddFilter<EventLogLoggerProvider>(
    "",
    LogLevel.Information);

var host = builder.Build();
host.Run();
