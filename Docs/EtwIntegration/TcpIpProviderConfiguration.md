# TcpIp Provider Configuration

## 🎯 Scopo

Il modulo `TcpIpProviderConfiguration` è responsabile dell'abilitazione del provider ETW manifest-based:

```
Microsoft-Windows-TCPIP
```

sulla sessione ETW creata da `EtwSessionController`.

Questo provider espone **eventi semantici** relativi al lifecycle delle connessioni TCP generati direttamente dalla **TCP state machine** all'interno del driver kernel:

```
tcpip.sys
```

### Capacità

Il modulo consente di:
- ✔️ Osservare connessioni TCP stabilite
- ✔️ Rilevare initiator e listener
- ✔️ Identificare eventi di disconnect (FIN / RST)
- ✔️ Ricostruire start e end delle connessioni
- ✔️ Alimentare algoritmi di detection

---

## 🧠 Modello di Osservabilità

A differenza dei kernel providers legacy (abilitati tramite `EnableFlags`), il provider:

```
Microsoft-Windows-TCPIP
```

è:
- **Manifest-based** → Versionabile
- **TDH-compatible** → Type information disponibile
- **Session-scoped** → Isolato per sessione

### Implicazioni

| Aspetto | Beneficio |
|---|---|
| Tipizzazione eventi | Parsing strutturato |
| Abilitazione per-sessione | Isolamento da altri consumer |
| Isolamento sessione | No interferenza con Windows Defender |
| Stop sessione sicuro | No impatti globali |

⚠️ **Importante:** Gli eventi generati rappresentano:
→ **TCP state transitions**

E **NON** singoli segmenti di rete.

---

## ⚙️ Runtime Filtering (EnableTraceEx2)

L'abilitazione del provider avviene tramite:

```c#
EnableTraceEx2()
```

Questa API non si limita ad abilitare il provider ma **configura anche il filtro di emissione lato kernel** tramite:

- `Level`
- `MatchAnyKeyword`
- `MatchAllKeyword`

### Comportamento Critico

Solo gli eventi che **soddisfano questi criteri** verranno emessi nella sessione.

Un set errato di keyword può causare:
- ✔️ Provider correttamente abilitato
- ❌ Assenza totale di eventi
- ❌ Nessun errore o warning runtime

⚠️ **Per questo motivo la configurazione di `MatchAnyKeyword` è parte critica della pipeline di ingestione.**

---

## 🧱 Componenti del Modulo

### 🔹 TcpIpProviderDescriptor

Rappresenta la **configurazione statica** del provider ETW.

| Campo | Descrizione |
|---|---|
| `ProviderGuid` | GUID del provider `Microsoft-Windows-TCPIP` |
| `Level` | `TRACE_LEVEL_INFORMATION` |
| `Keywords` | TCP lifecycle events |

Il descriptor viene utilizzato dal configuratore per **definire il filtro di emissione lato kernel**.

---

### 🔹 IEtwProviderConfigurator

Definisce il **contratto** per l'abilitazione di un provider manifest-based su una sessione ETW attiva.

**Responsabilità:**
- ✔️ Abilitare il provider tramite `EnableTraceEx2`
- ✔️ Configurare level e keyword
- ✔️ Applicare parametri avanzati di enablement

**NON deve:**
- ❌ Consumare eventi
- ❌ Parsare payload ETW
- ❌ Conoscere la pipeline
- ❌ Gestire persistenza

---

### 🔹 TcpIpProviderConfigurator

Implementa l'abilitazione del provider `Microsoft-Windows-TCPIP`.

**Chiama:**
```
EnableTraceEx2()
```

**Configurando:**
- `Level`
- `MatchAnyKeyword`
- `ENABLE_TRACE_PARAMETERS`

---

## 🔐 PID Reuse Mitigation

Il configuratore abilita:

```
EVENT_ENABLE_PROPERTY_PROCESS_START_KEY
```

tramite:

```
ENABLE_TRACE_PARAMETERS.EnableProperty
```

Questo **richiede al kernel** di arricchire gli eventi TCP con una:

→ **Process Start Key**

identificatore **monotonicamente crescente** assegnato al processo alla creazione.

### Perché è Fondamentale?

- 🔄 I PID possono essere **riutilizzati**
- ⏱️ Una connessione TCP può durare **più del processo che l'ha creata**
- 📤 Eventi di rundown possono arrivare **post-termination**

**Senza questa correlazione stabile si rischia di:**
- ❌ Attribuire una connessione al processo errato
- ❌ Corrompere la ricostruzione del lifecycle TCP
- ❌ Generare Community ID associati al soggetto sbagliato

---

## 🧭 Sequenza di Inizializzazione

```
EtwSessionController.StartOrAttach()
            ↓
TcpIpProviderConfigurator.EnableProvider()
            ↓
RealtimeConsumer.OpenTrace()
            ↓
         ✅ Risultato
```

### Stato Post-Abilitazione

Dopo l'abilitazione:
- ✔️ Il provider TCPIP è **attivo sulla sessione**
- ✔️ Gli eventi di lifecycle TCP sono **disponibili in realtime**
- ✔️ Il flusso è **TDH-compatible**
- ✔️ La correlazione **PID → processo** è **reuse-safe**