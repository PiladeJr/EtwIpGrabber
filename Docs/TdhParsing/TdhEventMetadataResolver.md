# TdhEventMetadataResolver

## 📌 Responsibility
La classe `TdhEventMetadataResolver` è responsabile della risoluzione dinamica del metadata TDH associato ad un evento ETW manifest-based.

Rappresenta il primo step della pipeline di parsing TDH, effettuando la trasformazione:
```text
EVENT_RECORD (runtime) → TRACE_EVENT_INFO (manifest metadata)
```

L'output prodotto da questa fase verrà utilizzato nelle fasi successive di:
- Layout Discovery
- Sequential Property Decoding

## 📥 Input
La classe riceve in input:
- `TDH_EVENT_RECORD*`

Questo record non proviene direttamente dalla callback ETW, ma è stato precedentemente:
- snapshot-tato durante la `EventRecordCallback`
- ricostruito runtime tramite `EventRecordReplayContext`
- pinned (`UserData`, `ExtendedData`)

La struttura costituisce una rappresentazione runtime del EVENT_RECORD originale,
ricostruita a partire dallo snapshot effettuato nella callback ETW. Non si tratta di una struttura nativa.

⚠️ TDH_EVENT_RECORD non è semanticamente equivalente a EVENT_RECORD,
ma ne replica il layout necessario per l'invocazione di specifiche API TDH
(es. TdhGetEventInformation).

Non tutte le API TDH accettano TDH_EVENT_RECORD in modo trasparente;
alcune (es. TdhFormatProperty) operano esclusivamente su payload e metadata
e non richiedono l'intero EVENT_RECORD.

Il record contiene:

| Campo | Descrizione |
|---|---|
| `EventHeader` | Identifica Provider/EventId/Version/Opcode |
| `UserData` | Payload runtime dell'evento |
| `UserDataLength` | Dimensione del payload |
| `ExtendedData` | Informazioni opzionali del kernel |

Il replay record deve essere:
- ABI-compatible con `EVENT_RECORD`
- allocato su stack
- contenere puntatori pinned

In quanto TDH accede direttamente ai campi tramite dereferenziazione.

## 🔄 Transformation
Dato un `TDH_EVENT_RECORD*`, la classe:
- estrae una chiave univoca runtime (`TdhEventKey`):
  - `ProviderId`
  - `EventId`
  - `Version`
  - `Opcode`
- verifica la presenza del metadata nel `TraceEventInfoBufferPool`

In caso di cache miss:
- invoca `TdhGetEventInformation(EVENT_RECORD*)`
- usa il pattern a due fasi richiesto:
  - query dimensione buffer
  - allocazione unmanaged
  - fetch del metadata
- memorizza il risultato nel pool

## 📤 Output
La classe restituisce:
- `TraceEventInfoHandle`

Contenente:
- `TRACE_EVENT_INFO`
- `EVENT_PROPERTY_INFO[]`
- string table

Questo buffer rappresenta il metadata manifest-based dell'evento runtime e contiene:

| Campo | Utilizzo |
|---|---|
| `PropertyCount` | Iterazione proprietà |
| `InType` | Tipo dati runtime |
| `OutType` | Tipo di output |
| `MapNameOffset` | Lookup map |
| `NameOffset` | Nome proprietà |

L'output viene utilizzato nella fase successiva di:
- Layout Discovery

Per effettuare il:
- Runtime Property Walk

E costruire:
- `TcpEventLayout`

## 🤝 Collaborators
La classe collabora con:

| Classe | Ruolo |
|---|---|
| `EventRecordReplayContext` | Ricostruzione runtime `EVENT_RECORD` |
| `TraceEventInfoBufferPool` | Caching metadata TDH |
| `TdhNativeMethods` | Invocazione API native |
| `TdhEventKey` | Discriminazione version-aware |

## ⚠️ Possibili errori e cause
| Sintomo | Possibile causa |
|---|---|
| `ERROR_INVALID_PARAMETER (87)` | P/Invoke ABI mismatch |
| `AccessViolationException` | Snapshot non pinned |
| Metadata mismatch | Manifest drift |
| Decode fallito a valle | Metadata non version-aware |

## 🗺️ Pipeline position
```text
EventRecordSnapshot
    ↓
EventRecordReplayContext
    ↓
TdhEventMetadataResolver
    ↓
TRACE_EVENT_INFO
    ↓
TcpEventLayoutBuilder
```

## Prossimo step

Una volta ottenuto il `TRACE_EVENT_INFO`, il payload dell'evento
non può essere ancora decodificato direttamente.

Il metadata deve essere analizzato per determinare:

- il numero di proprietà esposte
- il loro ordine runtime
- il tipo di ciascuna proprietà (InType / OutType)
- eventuali mapping associati

Questo processo, noto come Runtime Property Walk,
consente di ricostruire il layout effettivo dell'evento
a partire dal manifest TDH.

La fase successiva è quindi affidata alla classe:

- `TcpEventLayoutBuilder`

responsabile della trasformazione:

```text
TRACE_EVENT_INFO → TcpEventLayout
```