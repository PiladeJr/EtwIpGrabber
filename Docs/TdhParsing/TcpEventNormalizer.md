# TcpEventNormalizer

## 📌 Responsibility
La classe `TcpEventNormalizer` rappresenta lo step finale della pipeline di parsing TDH.

Il suo compito è trasformare il risultato del decoding sequenziale:
```text
RawTcpDecodedEvent (runtime decoded payload)
    ↓
TcpEvent (normalized connection event)
```

Durante questa fase, i dati ottenuti dal payload ETW vengono:
- convertiti da network byte order a host byte order
- tradotti in formati leggibili (IP, porte)
- mappati in enum di dominio
- arricchiti con timestamp e tipo evento

## 📥 Input
La classe riceve in input:

| Parametro | Descrizione |
|---|---|
| `RawTcpDecodedEvent` | Struttura prodotta dal `SequentialTdhDecoder` |
| `EVENT_HEADER` | Header dell'evento ETW |

La struttura `RawTcpDecodedEvent` contiene i valori estratti dal payload runtime ETW:

| Campo | Valore Raw | Stato |
|---|---|---|
| `LocalAddress` | 16777343 | Network byte order |
| `RemoteAddress` | 1392879808 | Network byte order |
| `LocalPort` | 20480 | Network byte order |
| `RemotePort` | 36895 | Network byte order |
| `AddressFamily` | 2 | Codice famiglia indirizzi |
| `Direction` | 1 | Codice direzione |
| `TcpFlags` | 2 | Codice flags TCP |

Valori numerici ancora:
- in network byte order
- non semanticamente interpretabili
- non adatti ad analisi di rete

## 🔄 Transformation
Dato un `RawTcpDecodedEvent` e un `EVENT_HEADER`, la classe effettua i seguenti passaggi di conversione:

**1. Timestamp conversion**

Il timestamp ETW viene fornito come `FILETIME` (100ns ticks).

Conversione:
```csharp
TimestampUtc = ConversionUtil.ConvertTimestamp(header.TimeStamp);
```

Esempio:
- Raw: `133456789123456789`
- → `2025-02-10 13:24:56.123Z`

**2. IPv4 byte-order normalization**

Gli indirizzi IP sono serializzati in big-endian.

```csharp
LocalIP  = ConversionUtil.Ntohl(raw.LocalAddress);
RemoteIP = ConversionUtil.Ntohl(raw.RemoteAddress);
```

Esempio:
- Raw `LocalAddress`: `16777343`
- → `127.0.0.1`

**3. TCP Port normalization**

Le porte vengono convertite allo stesso modo:

```csharp
LocalPort  = ConversionUtil.Ntohs(raw.LocalPort);
RemotePort = ConversionUtil.Ntohs(raw.RemotePort);
```

Esempio:
- Raw `LocalPort`: `20480`
- → `80`

**4. Direction decoding**

Il campo `Direction` viene mappato nell'enum di dominio:

```csharp
Direction = ConversionUtil.DecodeDirection(raw.Direction);
```

Esempio:
- Raw: `1` → `TcpDirection.Outbound`
- Raw: `2` → `TcpDirection.Inbound`

**5. TCP Flags decoding**

```csharp
Flags = ConversionUtil.DecodeFlags(raw.TcpFlags);
```

Esempio:
- Raw: `2` → `TcpFlags.ACK`

**6. Event type mapping**

L'`EventDescriptor.Id` identifica il tipo di evento TCP:

```csharp
EventType = ConversionUtil.MapEventType(header.EventDescriptor);
```

Esempio:
- EventId: `10` → `TcpEventType.Connect`
- EventId: `11` → `TcpEventType.Accept`
- EventId: `12` → `TcpEventType.Disconnect`

## 📤 Output
Il risultato della normalizzazione è un oggetto:
- `TcpEvent`

Esempio finale:
```text
TCP Connect
127.0.0.1:80 → 192.168.1.42:51344
ProcessId: 4321
Direction: Outbound
Flags: ACK
TimestampUtc: 2025-02-10T13:24:56Z
```

Questo oggetto rappresenta ora una connessione TCP:
- semanticamente interpretabile
- indipendente dal runtime ETW
- pronta per ulteriori elaborazioni

## 🤝 Collaborators
La classe collabora con:

| Classe | Ruolo |
|---|---|
| `ConversionUtil` | Funzioni di conversione da raw a normalizzato|
| `TcpEvent` e sottotipi | Oggetto normalizzato |

## ⚠️ Possibili errori e cause
| Sintomo | Possibile causa |
|---|---|
| Indirizzi IP invertiti | Endianness non convertito |
| Porte sbagliate | Network byte order non rispettato |
| Direction/Flags non riconosciuti | Mapping enum incompleto |
| Timestamp non valido | FILETIME corrotto |
| EventType sconosciuto | EventDescriptor non mappato |

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
    ↓
RawTcpDecodedEvent
    ↓
TcpEventNormalizer
    ↓
TcpEvent
```

## Prossimo step

Una volta ottenuto un `TcpEvent`, è disponibile un oggetto semanticamente significativo per il dominio applicativo.

Da questo punto è possibile:
- monitorare le connessioni di rete in tempo reale
- ricostruire la lifecycle di una connessione TCP
- distinguere traffico interno/esterno
- persistere le connessioni su database
- calcolare il Community ID
- correlare eventi tra host diversi