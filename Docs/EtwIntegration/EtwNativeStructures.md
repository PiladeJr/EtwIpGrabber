# ETW Native Structures

## 🎯 Scopo
La consumazione realtime di eventi ETW tramite le API native:
- `OpenTrace()`
- `ProcessTrace()`

richiede la definizione in user-mode di una serie di strutture **ABI-compatible** con il runtime ETW del kernel.

⚠️ **Fondamentale:** Queste strutture NON sono semplici DTO, ma costituiscono il **contratto binario (ABI)** tra:
```
advapi32.dll (user-mode)
          ↓
     ETW Runtime
          ↓
   ntoskrnl.exe (kernel)
```

## 🚨 Conseguenze di un Layout Errato

Un layout errato di anche una sola struttura causa:
- **Heap corruption**
- **Access violation**
- **Stack buffer overrun**
- **Crash non deterministici** durante `ProcessTrace()`

| Codice Errore | Significato |
|---|---|
| `0xC0000005` | Access Violation |
| `0xC0000374` | Heap Corruption |
| `0xC0000409` | Stack Buffer Overrun |

## 🧱 Strutture Richieste dal Consumer Realtime

### 🔹 EVENT_TRACE_LOGFILE
Struttura principale utilizzata da `OpenTrace()` per collegare il consumer user-mode ad una sessione ETW attiva.

**Contiene:**
- Nome sessione (`LoggerName`)
- Modalità di consumo (`ProcessTraceMode`)
- Callback (`EventRecordCallback`)
- Buffer interni utilizzati da ETW runtime

⚠️ **Critico - Memory Layout:**

Il runtime ETW scrive **direttamente in memoria** all'interno di:
- `CurrentEventRecord`
- `LogfileHeader`

durante l'esecuzione di `ProcessTrace()`

Questi campi **devono esistere** e avere **dimensioni corrette**, anche se:
- Non vengono mai letti dal codice managed
- Non sono logicamente necessari all'applicazione

La loro **assenza o dimensione errata** causa **scritture fuori dai limiti** da parte del runtime ETW.

---

### 🔹 EVENT_RECORD
Rappresenta l'evento ETW consegnato alla callback `EVENT_RECORD_CALLBACK`.

**Contiene:**
- Header evento (`EVENT_HEADER`)
- Contesto buffer (`ETW_BUFFER_CONTEXT`)
- Payload utente (`UserData`)
- Metadata opzionali (`ExtendedData`)

⚠️ **Importante:** Il puntatore a questa struttura è valido **solo durante l'esecuzione della callback** e deve essere **copiato immediatamente** in memoria managed.

---

### 🔹 EVENT_HEADER
Header standard di tutti gli eventi ETW manifest-based.

**Espone:**
- PID / TID
- Timestamp
- Provider GUID
- Opcode
- Task
- Keywords

**Usati per:**
- Parsing TDH
- Lifecycle reconstruction TCP
- Correlazione processo ↔ connessione

---

### 🔹 EVENT_DESCRIPTOR
Descrive semanticamente l'evento ETW.

**Campi:**
- EventId
- Version
- Level
- Opcode
- Task
- Keyword

**Costituisce la chiave primaria per:** `TdhGetEventInformation()` nella fase di parsing successiva.

---

### 🔹 ETW_BUFFER_CONTEXT
Indica il contesto di esecuzione dell'evento.

**Fornisce:**
- CPU che ha generato l'evento
- LoggerId della sessione

**Usato per:**
- Ordering e correlazione multi-CPU

---

### 🔹 EVENT_HEADER_EXTENDED_DATA_ITEM
Contiene metadata opzionali associati all'evento.

**Esempi:**
- Process Start Key
- Stack Trace
- SID
- TSID

**Richiesti per:**
- PID reuse mitigation
- Popolati solo se richiesti tramite `EnableTraceEx2` → `ENABLE_TRACE_PARAMETERS.EnableProperty`

---

### 🔹 TRACE_LOGFILE_HEADER
Popolato automaticamente dal runtime ETW durante `OpenTrace()`.

**Contiene informazioni globali sulla sessione:**
- Numero di CPU
- Clock frequency
- Modalità di logging
- Boot time

⚠️ Anche se non utilizzato direttamente, il campo **deve essere presente** per garantire il corretto offset dei campi successivi.

---

## 🔗 Perché Devono Essere Dichiarate Anche se Non Utilizzate?

ETW utilizza:
- **memcpy diretto** in user-mode per popolare le strutture fornite tramite `EVENT_TRACE_LOGFILE`

Il runtime ETW:
- ❌ NON effettua reflection
- ❌ NON verifica dimensioni
- ❌ NON valida offset
- ✔️ Assume che il layout sia **perfettamente compatibile** con quello definito in `evntrace.h`

Se una struct:
- È mancante
- Ha packing errato
- Ha dimensione diversa

ETW **continuerà comunque a scrivere in memoria** basandosi sugli offset attesi, causando:
→ **Memory corruption** lato processo consumer

---

## 🧨 Failure Modes e Sintomi

| Sintomo | Causa Probabile | Risoluzione |
|---|---|---|
| AccessViolation | Scrittura oltre buffer | Controllare dimensioni struct |
| Heap corruption | Offset errato in LogfileHeader | Verificare packing e alignment |
| BEX64 crash | Overrun dello stack | Verificare CurrentEventRecord |
| Crash in ProcessTrace | Callback pointer corrotto | Controllare EVENT_RECORD layout |
| ntdll.dll fault | Heap metadata invalidati | Controllare EVENT_TRACE_LOGFILE |

⚠️ **Nota:** Questi errori sono spesso:
- Non deterministici
- Dipendenti dal carico
- Difficili da riprodurre
- Manifestati dopo minuti di esecuzione

---

## 🧭 Note Importanti

Ogni struttura in questo documento rappresenta un **contratto binario inviolabile** con il kernel ETW.

Prima di modificare una struttura:
1. ✔️ Verificare lo schema in `evntrace.h`
2. ✔️ Controllare il packing (`#pragma pack`)
3. ✔️ Validare gli offset con `sizeof()`
4. ✔️ Testare con `ProcessTrace()` per memory corruption

Ogni deviazione dall'ABI atteso causa comportamento **non deterministico e crash silenti**.