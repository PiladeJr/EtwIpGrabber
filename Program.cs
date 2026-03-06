using EtwIpGrabber.EtwIntegration;
using EtwIpGrabber.EtwIntegration.EventDispatcher;
using EtwIpGrabber.EtwIntegration.MetricsAndHealth;
using EtwIpGrabber.EtwIntegration.ProviderConfiguration;
using EtwIpGrabber.EtwIntegration.ProviderConfiguration.Abstractions;
using EtwIpGrabber.EtwIntegration.RealTimeConsumer;
using EtwIpGrabber.EtwIntegration.SessionManager;
using EtwIpGrabber.EtwIntegration.SessionManager.Abstraction;
using EtwIpGrabber.EtwIntegration.SessionManager.Configuration;
using EtwIpGrabber.EtwIntegration.SessionManager.Configuration.Implementation;
using EtwIpGrabber.EtwIntegration.SessionManager.Native;
using EtwIpGrabber.PersistencyLayer.Filters;
using EtwIpGrabber.PersistencyLayer.Repository;
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
using EtwIpGrabber.Workers.Data;
using EtwIpGrabber.Workers.FanOut;
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
        SingleWriter = false
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

//------------------ Persistence layer -------------------

builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<ITcpConnectionRepository, TcpConnectionRepository>();

// filtro di persistenza: esclude tutte le connessioni non private 
builder.Services.AddSingleton<IPersistenceFilter>(
    new NetworkScopePersistenceFilter(
        NetworkScopeFilters.Private));

//------------------ Channel Workers ------------------

builder.Services.AddSingleton(
    new TcpLoggerChannel(
        Channel.CreateBounded<TcpConnectionLifecycle>(50000)));

builder.Services.AddSingleton(
    new TcpPersistenceChannel(
        Channel.CreateBounded<TcpConnectionLifecycle>(50000)));

//------------------ Workers ------------------
builder.Services.AddHostedService<DbInitializerWorker>();
builder.Services.AddHostedService<EtwCollectionWorker>();
builder.Services.AddHostedService<TcpParseWorker>();
builder.Services.AddHostedService<TcpLifecycleWorker>();
builder.Services.AddHostedService<TcpLifecycleFanOutWorker>();
builder.Services.AddHostedService<TcpPersistenceWorker>();
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