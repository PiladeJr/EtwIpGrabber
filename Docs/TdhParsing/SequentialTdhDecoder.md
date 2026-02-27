# SequentialTdhDecoder

## 📌 Responsibility
La classe `SequentialTdhDecoder` è responsabile del decoding sequenziale del payload binario associato ad un evento ETW TCPIP manifest-based.

Rappresenta il terzo step della pipeline di parsing TDH, effettuando la trasformazione:
```text
{TDH_EVENT_RECORD + TRACE_EVENT_INFO + TcpEventLayout}
                            ↓    
                   RawTcpDecodedEvent
```

L'output prodotto da questa fase verrà utilizzato nelle fasi successive di:
- Normalization
- Connection Lifecycle Reconstruction

## 📥 Input
La classe riceve in input:

| Parametro | Descrizione |
|---|---|
| `TDH_EVENT_RECORD*` | Evento runtime ABI-compatible |
| `TRACE_EVENT_INFO*` | Metadata manifest-based |
| `TcpEventLayout` | Decode plan runtime |

## 🔄 Transformation
Dato un `TDH_EVENT_RECORD*`, la classe:
- accede al payload runtime (`UserData`)
- recupera l'array `EVENT_PROPERTY_INFO[]`
- itera sulle proprietà definite nel manifest

Per ogni proprietà:
- invoca `TdhFormatProperty`
- ottiene:
  - valore formattato
  - byte consumati (`UserDataConsumed`)
- avanza l'offset nel payload: `offset += consumed`
- effettua binding runtime: `PropertyIndex → TcpEventLayout → RawTcpDecodedEvent`

Il decoding termina con successo se:
- `AddressFamily == AF_INET (2)`

**Nota:** Per questa implementazione specifica del servizio, eventi IPv6 vengono scartati.
motivo per cui viene effettuato un filtro su `AddressFamily` per includere solo eventi IPv4; 
restituendo false per eventi IPv6, che vengono ignorati nelle fasi successive.

## 📤 Output
La classe restituisce un oggetto:
- `RawTcpDecodedEvent`

Questa struttura contiene i valori estratti direttamente dal payload binario ETW. in particolare:

- indirizzi IPv4 locali e remoti
- porte TCP locali e remote
- Address Family
- ProcessId
- Direction
- TCP Flags

⚠️ **Importante:** I valori contenuti nella struttura sono ancora nel formato runtime
fornito dall’evento ETW. Non sono convertiti in host byte order e non sono semanticamente 
interpretabili

Ad esempio:

- gli indirizzi IPv4 sono in network byte order
- le porte TCP sono in network byte order
- il campo Direction rappresenta un valore numerico runtime
- i TCP Flags sono ancora in formato bitmask non normalizzato

Occorre un ulteriore fase di normalizzazione per trasformare questi valori in un formato semanticamente comprensibile

## 🤝 Collaborators
La classe collabora con:

| Classe | Ruolo |
|---|---|
| `TcpEventLayoutBuilder` | Costruzione layout runtime |
| `TcpEventLayout` | Decode plan |
| `TdhEventMetadataResolver` | Metadata TDH |
| `TdhNativeMethods` | Invocazione `TdhFormatProperty` |
| `TcpEventNormalizer` | Normalizzazione output |

## ⚠️ Possibili errori e cause
| Sintomo | Possibile causa |
|---|---|
| `ERROR_INVALID_PARAMETER (87)` | Offset errato o valori passati alla chiamata nativa non abi compatible|
| Decode fallito | Layout incompleto |
| Eventi IPv6 inclusi | AddressFamily non filtrato correttamente|
| `ERROR_INSUFFICIENT_BUFFER` | Buffer output insufficiente (se compare una volta per ciclo è un segno che il programma funziona correttamente) |
| Crash runtime | `TDH_EVENT_RECORD` non ABI-safe |

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
```

## Prossimo step

Una volta ottenuto il `RawTcpDecodedEvent`, è necessario convertire:
- Endianness
- TCP Flags
- Direction
- Timestamp

in un formato semanticamente utilizzabile.

Questo compito viene delegato alla classe:
- `TcpEventNormalizer`

responsabile della trasformazione:

```text
RawTcpDecodedEvent + EVENT_HEADER → TcpEvent
```

L'output finale `TcpEvent` sarà quindi disponibile per:
- Logging
- Persistenza
- Correlazione connessioni