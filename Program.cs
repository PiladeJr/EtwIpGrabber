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
using EtwIpGrabber.TcpLifeCycleReconstruction.Abstractions;
using EtwIpGrabber.TcpLifeCycleReconstruction.Finalization;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Reconstruction;
using EtwIpGrabber.TcpLifeCycleReconstruction.Storage;
using EtwIpGrabber.TcpLifeCycleReconstruction.Timeout;
using EtwIpGrabber.TcpLifeCycleReconstruction.Tracking;
using EtwIpGrabber.TdhParsing;
using EtwIpGrabber.TdhParsing.Decoder;
using EtwIpGrabber.TdhParsing.Decoder.Abstraction;
using EtwIpGrabber.TdhParsing.Layout;
using EtwIpGrabber.TdhParsing.Metadata;
using EtwIpGrabber.TdhParsing.Metadata.Abstract;
using EtwIpGrabber.TdhParsing.Normalization;
using EtwIpGrabber.TdhParsing.Normalization.Models;
using EtwIpGrabber.Utils.CommunityIdResolver;
using EtwIpGrabber.Utils.ProcessNameResolver;
using EtwIpGrabber.Workers;
using Microsoft.Extensions.Logging.EventLog;
using System.Threading.Channels;

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
builder.Services.AddSingleton<IProcessNameResolver, ProcessNameResolver>();
//------------------ Lifecycle reconstruction related ------------------

//=================================================//
// canale di eventi Tcp normalizzati in uscita dal //
// parser. Il canale è bounded per evitare un      //
// eccessivo consumo di memoria in caso di picchi. //
// TcpParseWorker è l'unico writer,                //
// TcpLifecycleWorker è l'unico reader.            //
//=================================================//
var tcpEventChannel = Channel.CreateBounded<TcpEvent>(
    new BoundedChannelOptions(100_000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = true
    });

builder.Services.AddSingleton(tcpEventChannel);
builder.Services.AddSingleton<ChannelReader<TcpEvent>>(tcpEventChannel.Reader);
builder.Services.AddSingleton<ChannelWriter<TcpEvent>>(tcpEventChannel.Writer);

//===============================================//
// Channel<TcpConnectionLifecycle>               //
// Writer: TcpTimeoutSweeper / Reconstructor     //
// Reader: TcpLifecycleLoggerWorker (future: DB) //
//===============================================//

var lifecycleChannel = Channel.CreateBounded<TcpConnectionLifecycle>(
    new BoundedChannelOptions(50_000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = true
    });

builder.Services.AddSingleton(lifecycleChannel);
builder.Services.AddSingleton<ChannelReader<TcpConnectionLifecycle>>(lifecycleChannel.Reader);
builder.Services.AddSingleton<ChannelWriter<TcpConnectionLifecycle>>(lifecycleChannel.Writer);

builder.Services.AddSingleton<ITcpFlowStore, ConcurrentTcpFlowStore>();
builder.Services.AddSingleton<TcpFlowReuseGuard>();
builder.Services.AddSingleton<ITcpFlowTracker, DefaultTcpFlowTracker>();
builder.Services.AddSingleton<ITcpLifecycleReconstructor, DefaultTcpLifecycleReconstructor>();
builder.Services.AddSingleton<ITcpConnectionFinalizer, TcpConnectionFinalizer>();
builder.Services.AddSingleton<ITcpTimeoutSweeper, TcpTimeoutSweeper>();
builder.Services.AddSingleton<ICommunityIdProvider, CommunityIdProvider>();
builder.Services.AddSingleton<CommunityIDGenerator>();

//------------------ Workers ------------------
builder.Services.AddHostedService<EtwCollectionWorker>();
builder.Services.AddHostedService<TcpParseWorker>();
builder.Services.AddHostedService<TcpLifecycleWorker>();
builder.Services.AddHostedService<TcpLifecycleLoggerWorker>();

//------------------ Logging ------------------
builder.Logging.ClearProviders();

builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "EtwIpGrabber";
    settings.LogName = "Application";
});

builder.Logging.AddFilter<EventLogLoggerProvider>(
    null,
    LogLevel.Information);
//------------------ Crash logging ------------------
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
//------------------ Build and run ------------------
var host = builder.Build();
host.Run();