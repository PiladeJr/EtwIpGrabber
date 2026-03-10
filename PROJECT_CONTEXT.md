# EtwIpGrabber - Project Context

## 📋 Overview
**EtwIpGrabber** è un servizio Windows in tempo reale per il monitoraggio delle connessioni TCP tramite Event Tracing for Windows (ETW). Il servizio intercetta, analizza e ricostruisce il ciclo di vita completo delle connessioni TCP, salvando i dati in un database SQLite locale per analisi successive.

### Scopo
- Sostituire meccanismi di parsing basati su netstat
- Integrare sistemi di analisi delle connessioni esistenti
- Fornire visibilità real-time del traffico TCP con dati arricchiti (Community ID, direzione, outcome, handshake stage)

---

## 🛠️ Stack Tecnologico

### Linguaggio e Framework
- **Linguaggio**: C# 12.0
- **Target Framework**: .NET 8.0 (Windows 10.0.17763.0+)
- **Tipo Progetto**: Windows Service (.NET Worker Service)
- **Platform**: Windows-only (richiede API ETW native)

### Dipendenze Principali
```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="10.0.3" />
<PackageReference Include="Microsoft.Diagnostics.Tracing.TraceEvent" Version="3.1.29" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.3" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.3" />
<PackageReference Include="Microsoft.Windows.SDK.Contracts" Version="10.0.26100.7705" />
<PackageReference Include="Microsoft.Windows.CsWinRT" Version="2.2.0" />
<PackageReference Include="NUnit" Version="4.5.0" />
```

### Features Abilitate
- **Nullable Reference Types**: `<Nullable>enable</Nullable>`
- **Implicit Usings**: `<ImplicitUsings>enable</ImplicitUsings>`
- **User Secrets**: Configurazione sicura tramite User Secrets
- **Unsafe Code**: Utilizzato per interoperabilità con API native ETW/TDH

---

## 🏗️ Struttura del Progetto

Il progetto è organizzato in moduli funzionali distinti:

### 1️⃣ **EtwIntegration** - ETW Core Components
Gestisce l'integrazione con Event Tracing for Windows (ETW).

**Responsabilità**:
- Gestione sessione ETW real-time
- Configurazione provider TCP/IP (`Microsoft-Windows-TCPIP`)
- Consumo eventi tramite callback native
- Buffering lock-free degli eventi

**Componenti chiave**:
```
EtwIntegration/
├── SessionManager/           # Controllo sessione ETW
│   ├── EtwSessionController.cs
│   ├── EtwSessionPropertiesFactory.cs
│   └── Configuration/
├── ProviderConfiguration/    # Configurazione provider TCP/IP
│   └── TcpIpProviderConfigurator.cs
├── RealTimeConsumer/         # Consumo eventi real-time
│   ├── RealtimeEtwConsumer.cs
│   └── Native/              # P/Invoke & strutture native
├── EventDispatcher/          # Buffering eventi
│   └── BoundedEventRingBuffer.cs
└── MetricsAndHealth/         # Telemetria
    └── EtwMetricsCollector.cs
```

**Pattern**:
- Uso intensivo di `unsafe` e `fixed` per gestione memoria nativa
- GC Handle pinning per callback ETW
- IDisposable pattern per cleanup risorse native

---

### 2️⃣ **TdhParsing** - Trace Data Helper Parsing
Decodifica il payload binario degli eventi ETW usando le API TDH (Trace Data Helper).

**Responsabilità**:
- Recupero metadata eventi ETW
- Parsing sequenziale proprietà manifest-based
- Normalizzazione dati in formato utilizzabile
- Conversione byte order (network → host)

**Componenti chiave**:
```
TdhParsing/
├── Metadata/                 # Recupero metadata TDH
│   ├── TdhEventMetadataResolver.cs
│   ├── TraceEventInfoBufferPool.cs
│   └── Native/              # P/Invoke TdhFormatProperty
├── Layout/                   # Layout runtime eventi
│   ├── TcpEventLayoutBuilder.cs
│   ├── TcpEventLayoutCache.cs
│   └── TcpEventLayout.cs
├── Decoder/                  # Decoding payload binario
│   ├── SequentialTdhDecoder.cs
│   └── RawTcpDecodedEvent.cs
├── Normalization/            # Normalizzazione dati
│   ├── TcpEventNormalizer.cs
│   ├── ConversionUtil.cs
│   └── Models/              # TcpEvent, TcpDirection, TcpFlags...
└── TcpEtwParser.cs          # Orchestratore parsing
```

**Flow**:
```
EVENT_RECORD (binario)
  ↓ TdhEventMetadataResolver
TRACE_EVENT_INFO (metadata)
  ↓ TcpEventLayoutBuilder
TcpEventLayout (indici proprietà)
  ↓ SequentialTdhDecoder
RawTcpDecodedEvent (byte order nativo)
  ↓ TcpEventNormalizer
TcpEvent (normalizzato, host byte order)
```

---

### 3️⃣ **TcpLifeCycleReconstruction** - Lifecycle Reconstruction
Ricostruisce il ciclo di vita completo delle connessioni TCP aggregando eventi individuali.

**Responsabilità**:
- Tracking flow attivi (tuple 5-tuple)
- Rilevamento inizio/fine connessione
- Determinazione outcome (Closed, Refused, Timeout, Established, Aborted)
- Calcolo handshake stage (SynSent, Established, Closing)
- Inferenza direzione (Inbound, Outbound, Local)
- Timeout sweeping per connessioni incomplete
- Generazione Community ID

**Componenti chiave**:
```
TcpLifeCycleReconstruction/
├── Models/
│   ├── TcpFlowInstance.cs           # Stato flow in corso
│   ├── TcpFlowKey.cs               # 5-tuple identifier
│   ├── TcpConnectionLifecycle.cs   # Risultato finale
│   └── Enumerations/
│       ├── TcpConnectionOutcome.cs
│       ├── TcpHandshakeStage.cs
│       └── TcpLifecycleState.cs
├── Storage/
│   └── ConcurrentTcpFlowStore.cs   # Store thread-safe
├── Tracking/
│   ├── DefaultTcpFlowTracker.cs
│   └── TcpFlowReuseGuard.cs        # Anti PID-reuse
├── Reconstruction/
│   └── DefaultTcpLifecycleReconstructor.cs
├── Finalization/
│   ├── TcpConnectionFinalizer.cs
│   └── CommunityIdProvider.cs      # RFC Community ID
└── Timeout/
    └── TcpTimeoutSweeper.cs        # Cleanup timeout (10s)
```

**Logica di determinazione**:
- **Outcome**: Inferito da sequenza eventi (Connect → Accept → Close/Disconnect)
- **Direction**: Eventi (Connect=Out, Accept=In) + euristica porte efimere
- **Stage**: Fase più avanzata osservata nel flow

---

### 4️⃣ **PersistencyLayer** - Database & Filtering
Gestisce la persistenza dei dati in SQLite con filtri configurabili.

**Responsabilità**:
- Inizializzazione schema database
- Persistenza flow attivi e lifecycle completati
- Filtering per scope di rete (Private, Public, All, Loopback)
- Routing a tabelle multiple per separazione dati
- View SQL per dati leggibili (enum int → text)

**Componenti chiave**:
```
PersistencyLayer/
├── Repository/
│   ├── DatabaseInitializer.cs      # Schema creation
│   ├── TcpConnectionRepository.cs  # Insert/Upsert
│   └── DbConfig.cs
└── Filters/
    ├── IPersistenceFilter.cs
    ├── PersistenceFilterImplementations.cs
    ├── NetworkScopeFilters.cs
    ├── PersistenceScopeResolver.cs # Parsing args
    └── TableResolver.cs            # Routing table
```

**Schema Database**:
```sql
-- Tabelle per scope (all, internal, public)
tcp_flows              # Flow attivi (upsert)
tcp_lifecycle          # Lifecycle completati (insert)
internal_tcp_flows
internal_tcp_lifecycle
public_tcp_flows
public_tcp_lifecycle

-- View unificata
v_all_tcp_lifecycle    # UNION ALL
v_tcp_lifecycle_readable # Enum mapping
```

**Filtri disponibili**:
- `Private`: Solo connessioni RFC 1918
- `Public`: Solo connessioni internet
- `All`: Tutte le connessioni
- `Loopback`: Solo 127.0.0.0/8

---

### 5️⃣ **Workers** - Background Services
Implementazione pipeline asincrona tramite `BackgroundService` e `System.Threading.Channels`.

**Workers**:
```
Workers/
├── EtwCollectionWorker.cs          # Avvio sessione ETW
├── TcpParseWorker.cs               # Parsing eventi
├── TcpLifecycleWorker.cs           # Ricostruzione + timeout sweep
├── TcpLifecycleFanOutWorker.cs     # Distribuzione multi-canale
├── TcpLifecycleLoggerWorker.cs     # Logging Windows Event Log
├── TcpLifecyclePersistenceWorker.cs # Persistenza lifecycle
├── TcpFlowPersistenceWorker.cs     # Persistenza flow
└── DbInitializerWorker.cs          # Init DB all'avvio
```

**Channel Pattern**:
```
BoundedEventRingBuffer (65K)
  ↓ (single writer/reader)
Channel<TcpEvent> (100K)
  ↓
Channel<TcpConnectionLifecycle> (50K) [single reader, multi writer]
  ↓ Fan-Out
  ├─→ TcpLoggerChannel
  └─→ TcpPersistenceChannel
```

---

### 6️⃣ **Utils** - Utility Services
Servizi di supporto cross-cutting.

```
Utils/
├── CommunityIdResolver/
│   └── CommunityIDGenerator.cs    # Community ID RFC
├── ProcessNameResolver/
│   └── ProcessNameResolver.cs     # PID → Process Name
└── ConnectionExtendedInfo/
    ├── NetworkClassification.cs   # IP classification
    ├── NetworkScope.cs
    └── ConnectionUtils.cs         # Util methods
```

---

## 🔄 Flusso di Esecuzione

### Pipeline Completa
```
1. ETW Startup
   ├─ EtwCollectionWorker crea sessione ETW
   ├─ Registra provider Microsoft-Windows-TCPIP
   └─ Avvia ProcessTrace loop su thread dedicato

2. Event Emission
   ├─ Callback nativa OnEventRecord
   ├─ Snapshot EVENT_RECORD (UserData copy)
   └─ BoundedEventRingBuffer.Enqueue

3. TDH Parsing
   ├─ TcpParseWorker dequeue snapshot
   ├─ TdhEventMetadataResolver → TRACE_EVENT_INFO
   ├─ TcpEventLayoutBuilder/Cache → TcpEventLayout
   ├─ SequentialTdhDecoder → RawTcpDecodedEvent
   ├─ TcpEventNormalizer → TcpEvent
   └─ Channel<TcpEvent>.Write

4. Lifecycle Reconstruction
   ├─ TcpLifecycleWorker legge TcpEvent
   ├─ DefaultTcpFlowTracker.OnEvent()
   │   ├─ GetOrCreate TcpFlowInstance
   │   ├─ Update flags (SeenConnect, SeenAccept...)
   │   └─ Check finalization criteria
   ├─ TcpConnectionFinalizer
   │   ├─ DetermineOutcome
   │   ├─ DetermineDirection
   │   ├─ DetermineStage
   │   └─ Generate Community ID
   └─ Channel<TcpConnectionLifecycle>.Write

5. Parallel Processing
   ├─ TcpLifecycleFanOutWorker → Fan-out a logger + DB
   ├─ TcpLifecycleLoggerWorker → Windows Event Log
   └─ TcpLifecyclePersistenceWorker
       ├─ Filter by NetworkScope
       ├─ Route to table (all/internal/public)
       └─ INSERT lifecycle

6. Timeout Sweeping
   └─ TcpLifecycleWorker (ogni 10s)
       ├─ TcpTimeoutSweeper.SweepAsync()
       ├─ Enumera flow attivi senza chiusura
       ├─ Finalizza come TcpConnectionOutcome.Timeout
       └─ Emit lifecycle
```

### Lifecycle States
```
Active → Finalizing → Finalized → TimedOut (se timeout)
```

---

## 📐 Convenzioni e Pattern

### Naming Conventions
- **Classi pubbliche**: `PascalCase`
- **Campi privati**: `_camelCase` (underscore prefix)
- **Interfacce**: `IPascalCase`
- **Enums**: `PascalCase` (members `PascalCase`)
- **Namespace**: `EtwIpGrabber.<Module>.<SubModule>`

### Design Patterns Utilizzati

#### 1. Dependency Injection
```csharp
// Constructor injection con primary constructor (C# 12)
internal sealed class TcpParseWorker(
    BoundedEventRingBuffer buffer,
    ITcpEtwParser parser,
    Channel<TcpEvent> channel,
    ILogger<TcpParseWorker> logger)
    : BackgroundService
```

#### 2. Repository Pattern
```csharp
ITcpConnectionRepository
  ├─ UpsertFlowAsync()      // Flow attivi
  └─ InsertLifecycleAsync() // Lifecycle completati
```

#### 3. Observer Pattern (Custom)
```csharp
ITcpEventObserver
  ├─ OnConnect()
  ├─ OnAccept()
  └─ OnClose()
```

#### 4. Object Pooling
```csharp
TraceEventInfoBufferPool // Buffer pool per TRACE_EVENT_INFO
ArrayPool<byte>          // Pool managed .NET
```

#### 5. Channel Pattern
```csharp
// Producer-Consumer con bounded channels
Channel.CreateBounded<T>(capacity, options)
  .Reader.ReadAllAsync()  // Consumer
  .Writer.WriteAsync()    // Producer
```

#### 6. Factory Pattern
```csharp
EtwSessionPropertiesFactory.Create()
TcpEventLayoutBuilder.Build()
```

#### 7. Strategy Pattern
```csharp
IPersistenceFilter
  ├─ NetworkScopePersistenceFilter (Private/Public/All)
  └─ Configurabile via args
```

### Code Style

#### Visibility
```csharp
// Default: internal sealed class (non ereditabile)
internal sealed class TcpParseWorker : BackgroundService

// Interfacce pubbliche per estensibilità
public interface ITcpEventObserver

// Struct per performance
internal struct RawTcpDecodedEvent

// Unsafe per interop nativo
public unsafe struct EVENT_TRACE_LOGFILE
{
    public fixed byte LogfileHeader[280];
}
```

#### Null Safety
```csharp
// Nullable enabled globalmente
#nullable enable

// Parametri out nullable
public bool TryParse(in EventRecordSnapshot snapshot, out TcpEvent? tcpEvent)

// Null-coalescing
var scope = args.FirstOrDefault() ?? NetworkScopeFilters.Private;
```

#### Error Handling
```csharp
// Try-pattern per parsing
if (!parser.TryParse(snapshot, out var tcp))
    continue;

// TaskCanceledException per graceful shutdown
catch (TaskCanceledException)
{
    // Expected during shutdown
}

// FailFast per errori critici
catch (AccessViolationException av)
{
    Environment.FailFast("TDH ABI violation", av);
}
```

#### Resource Management
```csharp
// IDisposable implementation
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
    
    // Cleanup native resources
    if (_traceHandle != 0)
        NativeEtwConsumer.CloseTrace(_traceHandle);
    
    // Wait for threads
    _processingThread?.Join(TimeSpan.FromSeconds(5));
}
```

#### Documentation
```csharp
/// <summary>
/// Breve descrizione classe/metodo.
/// </summary>
/// <remarks>
/// <para>
/// Dettagli implementativi, warning, edge cases.
/// </para>
/// <list type="bullet">
///   <item><description>Punto 1</description></item>
///   <item><description>Punto 2</description></item>
/// </list>
/// </remarks>
/// <param name="flow">Descrizione parametro</param>
/// <returns>Descrizione return</returns>
```

---

## 🚀 Deployment

### Installazione Servizio Windows
```cmd
sc.exe create EtwIpGrabber ^
    binPath= "C:\Path\To\EtwIpGrabber.exe --scope=Private" ^
    start= auto ^
    DisplayName= "ETW IP Connection Tracker"

sc.exe start EtwIpGrabber
```

### Argomenti Disponibili
```
--scope=<filter>    Filtro persistenza connessioni
  -s=<filter>       Alias breve

Valori:
  Private    (default) Solo connessioni RFC 1918
  Public     Solo connessioni internet
  All        Tutte le connessioni
  Loopback   Solo loopback (127.0.0.0/8)
```

### Logging
- **Windows Event Log**: `Application` → Source: `EtwIpGrabber`
- **Crash Log**: `<BaseDir>\tdh-crash.log`
- **Level**: `LogLevel.Information` (configurabile)

---

## 📊 Database Schema

### Tabelle Principali
```sql
-- Flow attivi (upsert by community_id)
tcp_flows (
    community_id TEXT PRIMARY KEY,
    process_id INTEGER,
    process_name TEXT,
    local_ip TEXT,
    local_port INTEGER,
    remote_ip TEXT,
    remote_port INTEGER,
    first_seen TEXT,      -- ISO 8601
    last_seen TEXT,
    flags INTEGER,        -- TcpFlowFlags bitmap
    state INTEGER         -- TcpLifecycleState enum
)

-- Lifecycle completati (insert con autoincrement)
tcp_lifecycle (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    community_id TEXT,
    process_id INTEGER,
    process_name TEXT,
    local_ip TEXT,
    local_port INTEGER,
    remote_ip TEXT,
    remote_port INTEGER,
    start_at TEXT,
    end_at TEXT,
    duration REAL,        -- Millisecondi
    weekday INTEGER,      -- 0=Sunday...6=Saturday
    hour INTEGER,         -- 0-23
    outcome INTEGER,      -- TcpConnectionOutcome enum
    handshake INTEGER,    -- TcpHandshakeStage enum
    direction INTEGER     -- TcpDirection enum
)
```

### View Unificata
```sql
CREATE VIEW v_tcp_lifecycle_readable AS
SELECT
    id,
    source_table,  -- 'all' | 'internal' | 'public'
    community_id,
    -- ...altri campi...
    CASE outcome
        WHEN 0 THEN 'Closed'
        WHEN 1 THEN 'Refused'
        WHEN 2 THEN 'Timeout'
        WHEN 3 THEN 'Established'
        WHEN 4 THEN 'Aborted'
        ELSE 'Unknown'
    END AS outcome,
    -- ...mapping direction, handshake...
FROM v_all_tcp_lifecycle;
```

### Query Esempio
```sql
-- Connessioni rifiutate nell'ultima ora
SELECT * FROM v_tcp_lifecycle_readable
WHERE outcome = 'Refused'
  AND start_at > datetime('now', '-1 hour')
ORDER BY start_at DESC;

-- Traffico per processo
SELECT 
    process_name,
    COUNT(*) as connections,
    AVG(duration) as avg_duration_ms
FROM tcp_lifecycle
GROUP BY process_name
ORDER BY connections DESC;

-- Connessioni per ora del giorno
SELECT 
    hour,
    COUNT(*) as count
FROM tcp_lifecycle
GROUP BY hour
ORDER BY hour;
```

---

## ⚠️ Limitazioni

### Piattaforma
- **Windows Only**: Richiede API ETW native (Windows 10 17763+)
- **TCP Only**: Provider UDP richiede estensioni
- **IPv4 Only**: Eventi IPv6 vengono filtrati

### Performance
- **Eventi Out-of-Order**: Il buffer ETW può emettere eventi non ordinati temporalmente
  - `Accept` può arrivare prima di `Connect`
  - Richiede meccanismi di ricostruzione tolleranti al disordine
- **Buffer Overflow**: Se `BoundedEventRingBuffer` è pieno, eventi vengono droppati
  - Monitorare `EtwMetricsCollector.DroppedEvents`

### Accuratezza
- **Direzione inferita**: Basata su euristica porte efimere (≥49152)
  - Potrebbe non essere accurata per custom port assignments
- **Handshake parziale**: Eventi possono non essere osservati se sessione ETW avviata a connessione in corso
- **PID Reuse**: Mitigato da `TcpFlowReuseGuard` ma non eliminato

---

## 🔌 Hook per Analisi Esterni

### Interfaccia Observer
```csharp
public interface ITcpEventObserver
{
    void OnConnect(in TcpEvent evt);
    void OnAccept(in TcpEvent evt);
    void OnDisconnect(in TcpEvent evt);
    void OnSend(in TcpEvent evt);
    void OnReceive(in TcpEvent evt);
}
```

### Implementazione Custom
```csharp
// 1. Implementa interfaccia
internal sealed class MyAnalysisHook : ITcpEventObserver
{
    public void OnConnect(in TcpEvent evt)
    {
        // Custom logic (es. invio a SIEM, ML model, etc.)
    }
    // ...altri metodi...
}

// 2. Registra in Program.cs
builder.Services.AddSingleton<ITcpEventObserver, MyAnalysisHook>();

// 3. Decommenta hook in DefaultTcpFlowTracker.cs
// _observer?.OnConnect(evt);
```

**Note**: I metodi hook sono commentati per evitare overhead se non utilizzati.

---

## 🔮 Estensioni Future

### 1. Provider UDP
**Necessario**:
- Estendere `IEtwProviderConfigurator` con `UdpProviderConfigurator`
- Creare `UdpEventLayout` + `UdpEventNormalizer`
- Modificare `TcpFlowKey` → `TransportFlowKey` (aggiungere campo protocol)
- Duplicare pipeline per eventi UDP

**Esempio**:
```csharp
// Provider GUID: Microsoft-Windows-TCPIP (include UDP)
builder.Services.AddSingleton<IEtwProviderConfigurator, UdpProviderConfigurator>();
builder.Services.AddSingleton<IUdpEtwParser, UdpEtwParser>();
```

### 2. Event Ordering
**Problema**: Buffer ETW può emettere eventi non ordinati.

**Soluzione proposta**:
```csharp
// Buffering con riordinamento temporale
internal sealed class TemporalEventSorter
{
    private readonly SortedList<DateTime, EventRecordSnapshot> _buffer;
    private readonly TimeSpan _window = TimeSpan.FromMilliseconds(100);
    
    public void Enqueue(EventRecordSnapshot snapshot)
    {
        _buffer.Add(snapshot.Header.TimeStamp, snapshot);
        FlushExpired();
    }
}
```

### 3. Visualizzazione Custom
**Proposta**: Console UI invece di Event Log

```csharp
// Custom command: EtwIpGrabber.exe --show
builder.Services.AddHostedService<ConsoleUIWorker>();

// Output stile htop/tmux
┌─ Active Flows: 42 ───────────────────────────────┐
│ PID   Process        Local              Remote   │
│ 1234  chrome.exe     :443 → 1.1.1.1:443          │
│ 5678  teams.exe      :5000 ← 10.0.0.5:49152      │
└──────────────────────────────────────────────────┘
```

---

## 📚 Documentazione

La documentazione dettagliata per ogni modulo è disponibile nella cartella `Docs/`:

```
Docs/
├── EtwIntegration/
│   ├── EtwSessionController.md
│   ├── EtwNativeStructures.md
│   └── TcpIpProviderConfiguration.md
├── TdhParsing/
│   ├── TcpEventLayoutBuilder.md
│   ├── SequentialTdhDecoder.md
│   └── TcpEventNormalizer.md
└── TcpLifeCycleReconstruction/
    ├── FlowTracking.md
    ├── LifecycleReconstruction.md
    └── OutcomeInference.md
```

**Per approfondire**:
- Flow tracking → `Docs/TcpLifeCycleReconstruction/FlowTracking.md`
- TDH parsing → `Docs/TdhParsing/SequentialTdhDecoder.md`
- ETW interop → `Docs/EtwIntegration/EtwNativeStructures.md`

---

## 🤝 Contributi

### Best Practices
1. **Unsafe code**: Solo per interop nativo, mai per ottimizzazioni premature
2. **Null safety**: Sempre usare `?` per reference type nullable
3. **Logging**: `LogError` per eccezioni, `LogWarning` per anomalie, `LogInformation` per eventi normali
4. **Testing**: Aggiungere unit test in `Utils/CommunityIdResolver/CommunityIdGeneratorTests.cs`
5. **Documentation**: XML doc completo per classi pubbliche e internal

### PR Checklist
- [ ] Build succeed senza warning
- [ ] Documentazione XML aggiornata
- [ ] Gestione corretta risorse native (IDisposable)
- [ ] Graceful shutdown (check `CancellationToken`)
- [ ] Logging appropriato

---

## 📝 License
[Specificare licenza del progetto]

## 👤 Autori
[Specificare autori/maintainer]

---

**Ultimo aggiornamento**: 2024-01-XX  
**Versione documento**: 1.0
