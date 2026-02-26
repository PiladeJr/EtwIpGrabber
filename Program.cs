using EtwIpGrabber;
using EtwIpGrabber.EtwStructure;
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
using EtwIpGrabber.TdhParsing;
using EtwIpGrabber.TdhParsing.Decoder;
using EtwIpGrabber.TdhParsing.Decoder.Abstraction;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Metadata;
using EtwIpGrabber.TdhParsing.Metadata.Abstract;
using EtwIpGrabber.TdhParsing.Normalization;
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

//------------------ ETW core components ------------------
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

//------------------ TDH Parsing related ------------------
builder.Services.AddSingleton<TraceEventInfoBufferPool>();
builder.Services.AddSingleton<IEtwMetadataResolver, TdhEventMetadataResolver>();

builder.Services.AddSingleton<TcpEventLayoutBuilder>();
builder.Services.AddSingleton<TcpEventLayoutCache>();

builder.Services.AddSingleton<ITdhDecoder, SequentialTdhDecoder>();
builder.Services.AddSingleton<TcpEventNormalizer>();

builder.Services.AddSingleton<ITcpEtwParser, TcpEtwParser>();

//------------------ Workers ------------------
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<TcpParseWorker>();

builder.Logging.ClearProviders();

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "EtwIpGrabber";
    settings.LogName = "Application";
});

builder.Logging.AddFilter<EventLogLoggerProvider>(
    "",
    LogLevel.Information);
// temporaneamente alzato a debug per investigare su un possibile problema di parsing, da rimuovere una volta risolto.
builder.Logging.AddFilter<EventLogLoggerProvider>(
    "",
    LogLevel.Debug);

var crashPath =
    Path.Combine(
        AppContext.BaseDirectory,
        "tdh-crash.log");

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    File.AppendAllText(
        crashPath,
        $"Unhandled: {e.ExceptionObject}\n");
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    File.AppendAllText(
        crashPath,
        $"TaskException: {e.Exception}\n");
};

var host = builder.Build();
host.Run();