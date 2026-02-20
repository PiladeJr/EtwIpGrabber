# RealtimeEtwConsumer

## 🎯 Scopo
Il modulo `RealtimeEtwConsumer` è responsabile del consumo in tempo reale degli eventi ETW generati dal provider:
- `Microsoft-Windows-TCPIP`

Abilitato sulla sessione ETW tramite il `TcpIpProviderConfigurator`.

Questo componente rappresenta il punto di ingresso della pipeline di raccolta degli eventi TCP lifecycle e consente di:
- collegarsi a una sessione ETW attiva
- ricevere eventi in streaming
- inoltrare gli eventi alla pipeline interna
- disaccoppiare il thread ETW dal motore di ricostruzione TCP

## 🧠 Modello di consumo realtime
Il consumo degli eventi avviene tramite le API native:
```text
OpenTrace()
ProcessTrace()
CloseTrace()
```

La sequenza operativa è:
```text
StartTrace()             → creazione sessione
    ↓
EnableTraceEx2()         → abilitazione provider
    ↓
OpenTrace()              → apertura consumer
    ↓
ProcessTrace()           → loop di ricezione eventi
    ↓
EventRecordCallback()    → consegna eventi
    ↓
Dispatcher.TryEnqueue()  → staging buffer
```

Gli eventi vengono consegnati alla callback registrata tramite:
- `EVENT_RECORD_CALLBACK`

Che viene eseguita nel contesto del thread ETW.

## ⚠ Thread ETW: vincoli operativi
La funzione di callback viene eseguita dal thread di consegna ETW.

Qualsiasi operazione bloccante eseguita in questo contesto può causare:
- saturazione dei buffer kernel
- incremento di `EventsLost`
- perdita di eventi TCP lifecycle
- degradazione della detection accuracy

Pertanto, la callback deve limitarsi a:
- inoltrare il puntatore `EVENT_RECORD` alla coda di staging

Senza:
- parsing TDH
- allocazioni dinamiche
- I/O sincrono
- lookup di processo

## 📦 Componenti del modulo
### `IRealtimeEtwConsumer`
Definisce il contratto per il consumo realtime degli eventi ETW da una sessione attiva.

Responsabilità:
- apertura della sessione tramite `OpenTrace`
- avvio del loop di ricezione tramite `ProcessTrace`
- inoltro degli eventi alla pipeline interna

### `NativeEtwConsumer`
Wrapper delle API ETW native:

| Metodo | Descrizione |
|---|---|
| `OpenTrace` | collega il consumer alla sessione ETW |
| `ProcessTrace` | avvia il loop di ricezione eventi |
| `CloseTrace` | chiude il consumer |

Il metodo `ProcessTrace` è bloccante e deve essere eseguito su un thread dedicato.

### `EventRecordCallback`
Delegate invocato da ETW per ogni evento ricevuto dalla sessione.

Riceve un puntatore alla struttura:
- `EVENT_RECORD`

Che rappresenta l'evento ETW in formato TDH-compatible.

Il delegate deve essere:
- pinned tramite `GCHandle`
- registrato tramite `Marshal.GetFunctionPointerForDelegate`

Per evitare invalidazione da parte del Garbage Collector.

### `IEventDispatcher`
Definisce il contratto per l'inoltro degli eventi dalla callback ETW al resto della pipeline.

Il dispatcher funge da:
- staging buffer

Tra:
- il thread ETW (producer)
- il motore di ricostruzione TCP (consumer)

### `LockFreeEventDispatcher`
Implementazione base del dispatcher che utilizza una `ConcurrentQueue` per accodare i puntatori agli eventi.

In questa fase:
- non viene effettuato parsing
- non viene applicato filtraggio
- non viene copiato il payload

Il dispatcher sarà successivamente sostituito con una struttura lock-free a dimensione fissa per gestire backpressure sotto carico elevato.

## 🧩 Strutture native utilizzate
Il corretto funzionamento di `OpenTrace()` e `ProcessTrace()` richiede la definizione di una serie di strutture native ETW:
- `EVENT_TRACE_LOGFILE`
- `EVENT_RECORD`
- `EVENT_HEADER`
- `ETW_BUFFER_CONTEXT`
- `EVENT_TRACE`
- `TRACE_LOGFILE_HEADER`

Queste strutture:
- definiscono il layout ABI atteso dal kernel
- contengono metadati relativi agli eventi
- permettono il parsing TDH del payload

Anche se alcune di esse (es. `EVENT_TRACE`) non vengono utilizzate direttamente in modalità `PROCESS_TRACE_MODE_EVENT_RECORD`, devono comunque essere definite per garantire il corretto layout della struttura `EVENT_TRACE_LOGFILE`.

## 🧭 Sequenza di inizializzazione
Il consumo degli eventi avviene dopo:

```text
EtwSessionController.StartOrAttach()
    ↓
TcpIpProviderConfigurator.EnableProvider()
    ↓
RealtimeEtwConsumer.Start()
    ↓
Dispatcher.TryEnqueue()
```

## ✅ Risultato
Dopo l'avvio del consumer:
- la sessione ETW viene consumata in tempo reale
- gli eventi TCP lifecycle sono disponibili
- la callback ETW resta non-bloccante
- la pipeline è pronta per il parsing TDH