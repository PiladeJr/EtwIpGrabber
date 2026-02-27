# TcpEventLayoutBuilder

## 📌 Responsibility
La classe `TcpEventLayoutBuilder` è responsabile della costruzione del layout runtime di decoding di un evento ETW TCPIP manifest-based.

Rappresenta il secondo step della pipeline di parsing TDH, effettuando la trasformazione:
```text
TRACE_EVENT_INFO (manifest metadata) → TcpEventLayout (runtime decode plan)
```

L'output prodotto da questa fase verrà utilizzato nelle fasi successive di:
- Sequential Property Decoding
- IPv4 Filtering
- Normalization

## 📥 Input
La classe riceve in input:
- `TRACE_EVENT_INFO*` (incapsulato in `IntPtr`)

Questo buffer:
- è stato precedentemente risolto tramite `TdhGetEventInformation`
- contiene il metadata manifest-based dell'evento runtime
- include:
  - intestazione `TRACE_EVENT_INFO`
  - array variabile di `EVENT_PROPERTY_INFO`
  - string table (nomi proprietà)

⚠️ **Importante:**

`TRACE_EVENT_INFO` è seguito in memoria da `EVENT_PROPERTY_INFO[PropertyCount]`.

Il layout deve quindi essere interpretato tramite:
- pointer arithmetic
- header alignment a 8 byte

## 🔄 Transformation
Dato un `TRACE_EVENT_INFO*`, la classe:
- allinea la dimensione dell'header (`TRACE_EVENT_INFO`)
- accede runtime all'array `EVENT_PROPERTY_INFO[]`
- itera sulle proprietà definite nel manifest
- estrae il nome di ogni proprietà tramite `NameOffset`
- effettua binding: `PropertyName → PayloadIndex`
- popola una struttura `TcpEventLayout`

Gestisce alias runtime tra versioni Windows:

| Windows 10 | Windows 11 |
|---|---|
| `saddr` | `LocalAddress` |
| `daddr` | `RemoteAddress` |
| `sport` | `LocalPort` |
| `dport` | `RemotePort` |
| `pid` | `ProcessId` |

Questo passaggio rappresenta il principale meccanismo di compatibilità:
- Windows 10 ↔ Windows 11 Manifest Drift

## 📤 Output
La classe restituisce:
- `TcpEventLayout`

Contenente:

| Campo | Utilizzo |
|---|---|
| `LocalAddressIndex` | Offset IPv4 locale |
| `RemoteAddressIndex` | Offset IPv4 remoto |
| `LocalPortIndex` | Porta locale |
| `RemotePortIndex` | Porta remota |
| `ProcessIdIndex` | PID processo |
| `AddressFamilyIndex` | Filtro IPv4 |
| `DirectionIndex` | Direzione connessione |
| `TcpFlagsIndex` | Flags TCP |
| `DirectionHasMap` | Lookup map runtime |
| `Supported` | Layout valido |

Il layout rappresenta un **Decode Plan Runtime**, utilizzato successivamente dal `SequentialTdhDecoder` per effettuare:
- Offset-aware Sequential Payload Walk

## 🤝 Collaborators
La classe collabora con:

| Classe | Ruolo |
|---|---|
| `TdhEventMetadataResolver` | Risoluzione metadata TDH |
| `TRACE_EVENT_INFO` | Metadata evento |
| `EVENT_PROPERTY_INFO` | Descrizione proprietà |
| `TcpEventLayoutCache` | Caching layout |
| `SequentialTdhDecoder` | Decoding payload |

## ⚠️ Possibili errori e cause
| Sintomo | Possibile causa |
|---|---|
| `layout.Supported = false` | Proprietà mancanti nel manifest |
| Decode fallito a valle | Alias non gestiti |
| IPv6 non filtrato | AddressFamily non bindato |
| `ERROR_INVALID_PARAMETER (87)` | Offset errato a valle |
| Eventi ignorati | Binding incompleto |

## 🗺️ Pipeline position
```text
TDH_EVENT_RECORD
    ↓
TdhEventMetadataResolver
    ↓
TRACE_EVENT_INFO
    ↓
TcpEventLayoutBuilder
    ↓
TcpEventLayout
    ↓
SequentialTdhDecoder
```

## Prossimo step

Una volta ottenuto il `TcpEventLayout`, è possibile effettuare il decoding sequenziale del payload evento tramite `TdhFormatProperty`.

Il metadata deve essere utilizzato per:
- avanzare offset nel payload
- estrarre proprietà IPv4
- filtrare `AddressFamily != AF_INET`
- produrre un `RawTcpDecodedEvent`

Questo compito viene delegato alla classe:
- `SequentialTdhDecoder`

responsabile della trasformazione:

```text
TcpEventLayout + UserData → RawTcpDecodedEvent