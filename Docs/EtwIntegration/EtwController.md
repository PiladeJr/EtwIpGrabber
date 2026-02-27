# EtwSessionController

## 🎯 Scopo della classe
`EtwSessionController` è il componente responsabile del lifecycle completo di una ETW realtime session utilizzata per la raccolta di eventi TCP/IP dal provider manifest based.
`Microsoft-Windows-TCPIP`.
Gestisce:
- Creazione sessione ETW
- Attach a sessione esistente (post crash)
- Stop deterministico della sessione
- Recovery-safe restart via SCM
- Gestione dell'handle kernel (`TRACEHANDLE`)

Questa classe rappresenta il confine tra:
```text
User-mode (.NET Service)
↓
ETW Session Manager
↓
Kernel ETW Runtime
```

La classe ha una sola responsabilabilità.
Gestire la creazione, il recupero e la chiusura di una ETW Session in modo crash-safe.

## 🧱 Componenti interni
### 🔹 `_config`
```c#
private readonly IEtwSessionConfig _config;
```

Configurazione immutabile della sessione ETW.

Contiene:
- Nome sessione
- Buffer sizing
- Flush timer
- Log mode (Realtime/SystemLogger)

Usata per:
- `StartTrace()`
- `ControlTrace()`

### 🔹 `_handle`
```c#
private readonly NativeEtwSessionHandle _handle;
```

Wrapper del:
- `TRACEHANDLE`

Restituito da:
- `StartTrace()`

Oppure recuperato via:
- `ControlTrace(EVENT_TRACE_CONTROL_QUERY)`

Rappresenta l'ETW Logger Context

Se perso:
- la sessione non può essere stoppata
- la sessione rimane attiva nel kernel
- StartTrace() successivi restituiscono:
`ERROR_ALREADY_EXISTS`
### 🔹 `_factory`
```c#
private readonly EtwSessionPropertiesFactory _factory;
```

Responsabile di creare:
- `EVENT_TRACE_PROPERTIES*`

In formato ABI-safe richiesto da ETW:
- `[ EVENT_TRACE_PROPERTIES ][ wchar sessionName ]`

In un unico blocco contiguo.

### 🔹 `_running`
```c#
private volatile bool _running;
```

Indica se il controller è attualmente attaccato ad una sessione ETW valida.

Serve a:
- prevenire double stop
- evitare invalid handle call
- garantire determinismo nello shutdown SCM

## 🔓 Proprietà pubbliche
### `SessionHandle`
```c#
public ulong SessionHandle => _handle.Handle;
```

Handle kernel della sessione ETW attiva.

Necessario per:
- `EnableTraceEx2()` durante l'abilitazione del provider TCPIP.

### `SessionName`
```c#
public string SessionName => _config.SessionName;
```

Nome della sessione registrata nel kernel ETW namespace.

Usato da `OpenTrace()` nel real time consumer per collegarsi alla sessione.

### `IsRunning`
```c#
public bool IsRunning => _running;
```

Indica se il controller è attualmente attivo.

## 🔁 Lifecycle methods
### ▶ `StartOrAttach()`
Responsabile di:
- Creare una nuova ETW session
- Oppure attaccarsi ad una esistente

Workflow:
1. `StartTrace()`
2. `SUCCESS` → nuova sessione
3. `ERROR_ALREADY_EXISTS`
4. `ControlTrace(EVENT_TRACE_CONTROL_QUERY)`
5. recupero `HistoricalContext`

Questo consente:
- recovery post crash
- restart SCM safe
- no duplicate session

### 🔗 `Attach()`
Usato quando:
- `StartTrace()` → `ERROR_ALREADY_EXISTS`

Chiama:
- `ControlTrace(... QUERY ...)`

Per ottenere:
- `WNODE_HEADER.HistoricalContext`

Che rappresenta il:
- `TRACEHANDLE` della sessione esistente.

Questo è fondamentale per:
- abilitare il provider TCPIP sulla sessione esistente
- Consentire lo `Stop()` successivo.

### ⏹ `Stop()`
Responsabile della chiusura deterministica della sessione ETW.

Chiama:
- `ControlTrace(... STOP ...)`

E gestisce:

| Return Code | Significato |
|---|---|
| `0` | stop riuscito |
| `4201` | già stoppata |

Qualsiasi altro errore:
- → sessione potenzialmente leakata
- → viene sollevata eccezione

### 🧹 `Dispose()`
Invoca:
- `Stop()`

Durante lo shutdown del Windows Service.

Questo garantisce:
- rilascio dell' ETW Session
- no orphan session
- possibilità di restart del servizio

## 🧨 Failure modes gestiti
| Scenario | Behaviour |
|---|---|
| Service crash | Attach |
| Service restart | Attach |
| Host reboot | Create |
| Session already exists | Attach |
| Handle lost | Query |
| Session already stopped | Safe |

## 🧭 Note importanti
- ETW Session NON è owned dal processo
- È una Kernel Global Resource

Può sopravvivere a:
- Service crash
- Process termination
- Unhandled exception

Per questo motivo:
- `Attach()` è obbligatorio.
- `Stop()` deve essere sincrono:
- SCM impone timeout allo shutdown.

Sessioni ETW lasciate aperte causano:
- `ERROR_ALREADY_EXISTS` al successivo `StartTrace()`