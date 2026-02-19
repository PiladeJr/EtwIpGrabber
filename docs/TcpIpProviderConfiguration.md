# TcpIpProviderConfiguration

## 🎯 Scopo
Il modulo `TcpIpProviderConfiguration` è responsabile dell’abilitazione del provider ETW manifest-based:
- `Microsoft-Windows-TCPIP`

Sulla sessione ETW creata da `EtwSessionController`.

Questo provider espone eventi semantici relativi al lifecycle delle connessioni TCP, generati direttamente dalla TCP state machine all'interno del driver kernel `tcpip.sys`.

Il modulo consente di:
- osservare connessioni TCP stabilite
- rilevare initiator e listener
- identificare eventi di disconnect (FIN / RST)
- ricostruire start e end delle connessioni
- alimentare algoritmi di detection

## 🧠 Modello di osservabilità
A differenza dei kernel providers legacy (abilitati tramite EnableFlags), il provider `Microsoft-Windows-TCPIP` è:
- manifest-based
- versionabile
- TDH-compatible
- session-scoped

Questo significa che:
- gli eventi sono tipizzati
- la sessione è isolata da altri consumer
- l’abilitazione non interferisce con altri servizi (es. Windows Defender)
- lo stop della sessione non ha impatti globali

Gli eventi generati rappresentano:
- `TCP state transitions`

E NON singoli segmenti di rete.

## 📦 Componenti del modulo
### `TcpIpProviderDescriptor`
Rappresenta la configurazione statica del provider ETW:
- GUID del provider
- livello minimo di tracing
- keyword da abilitare

Questo oggetto definisce:

| Campo | Descrizione |
|---|---|
| `ProviderGuid` | GUID del provider `Microsoft-Windows-TCPIP` |
| `Level` | `TRACE_LEVEL_INFORMATION` |
| `Keywords` | TCP lifecycle events |

Il descriptor è utilizzato dal configuratore per abilitare il provider sulla sessione ETW.

### `IEtwProviderConfigurator`
Definisce il contratto per l’abilitazione di un provider manifest-based su una sessione ETW attiva.

Responsabilità:
- abilitare il provider tramite `EnableTraceEx2`
- configurare level e keyword
- applicare parametri di enablement avanzati

NON deve:
- consumare eventi
- parsare payload ETW
- conoscere la pipeline
- gestire persistenza

### `TcpIpProviderConfigurator`
Implementa l’abilitazione del provider `Microsoft-Windows-TCPIP` sulla sessione ETW.

Chiama:
```text
EnableTraceEx2()
```

Specificando:
```text
Level
MatchAnyKeyword
ENABLE_TRACE_PARAMETERS
```

Per configurare il comportamento del provider nella sessione.

## 🔐 PID reuse mitigation
Il configuratore abilita:
```text
EVENT_ENABLE_PROPERTY_PROCESS_START_KEY
```

Tramite:
```text
ENABLE_TRACE_PARAMETERS.EnableProperty
```

Questo richiede al kernel di arricchire gli eventi TCP con una:
- Process Start Key

Un identificatore monotonicamente crescente associato al processo al momento della creazione.

Questo è fondamentale perché:
- i PID possono essere riutilizzati dal sistema
- una connessione TCP può durare più del processo che l’ha creata
- senza correlazione stabile si rischia di associare la connessione a un processo errato

L’uso della Process Start Key consente:
- correlazione PID → processo affidabile
- maggiore accuratezza nella detection
- integrità del Community ID associato al processo

## 🧭 Sequenza di inizializzazione
L’abilitazione del provider avviene dopo la creazione o il recupero della sessione ETW:

```text
EtwSessionController.StartOrAttach()
    ↓
TcpIpProviderConfigurator.EnableProvider()
    ↓
RealtimeConsumer.OpenTrace()
```

## ✅ Risultato
Dopo l’abilitazione:
- il provider `Microsoft-Windows-TCPIP` è attivo sulla sessione
- gli eventi di lifecycle TCP sono disponibili in realtime
- il flusso è TDH-compatible
- la correlazione con il processo è PID-safe