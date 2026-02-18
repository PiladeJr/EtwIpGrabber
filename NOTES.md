ETW: Buffer Overflow → Event Loss

Cosa succede realmente

ETW usa:

- per-CPU nonpaged circular buffers

Quando:

- Producer rate > Consumer drain rate

il kernel:

- droppa eventi silenziosamente

Non esiste backpressure verso:

- `tcpip.sys`

Il network stack non rallenta per ETW.

Failure Mode sulla Ricostruzione TCP

ETW TCP lifecycle è:

- transition-driven

Perdi anche un solo evento e ottieni:

| Evento perso | Effetto |
|---|---|
| TcpConnect | connessione senza start |
| TcpAccept | server-side invisibile |
| Disconnect | zombie flow |
| RST | close reason errato |
| Timeout | lifetime infinito |

Il risultato downstream sarà:

- StartTime corretto
- EndTime mancante

o:

- flow aperto per ore

→ Feature ML:

- duration
- connection count
- fan-out

diventano distorte.

Come si manifesta

Kernel genera:

- `EVENT_TRACE_LOST_EVENT`
- `EVENT_TRACE_BUFFER_LOST`

ma:

⚠️ NON per ogni flow corrotto


🔴 2. Provider Schema Change

Il provider:

- Microsoft-Windows-TCPIP

è:

- Manifest-based
- NON ABI-stable

Il campo:

- EventID = 261

su:

- Windows 10 1909

può:

- cambiare struttura
- aggiungere campo
- cambiare semantic meaning

su:

- Windows 11 23H2

Failure Mode

La tua pipeline:

- EventID → Lifecycle transition

può iniziare a:

- interpretare ConnectFailure come ConnectSuccess
- scambiare FIN con Abort
- perdere IPv4 fields

senza crash → silent semantic drift

Il tuo database rimane coerente
ma il significato cambia.

Dove documentarti

- "TCPIP ETW Provider Manifest History"

- `netsh trace show provider Microsoft-Windows-TCPIP`

- Windows SDK:
  `%ProgramFiles(x86)%\Windows Kits\10\Include\<version>\um\TcpEtw.man`

Libro:

- Windows Internals Part 2 – Networking Instrumentation


🔴 3. Windows Build Variance

Windows networking stack evolve:

- RACK
- TFO
- Hybrid close
- SYN data
- Fast open retry

Nuovi path TCP:

- SYN → DATA → ACK

possono generare:

- meno eventi
- eventi in ordine diverso
- Accept senza Connect

Failure Mode

Il tuo correlator assume:

- Connect → Established → Disconnect

ma su build recenti:

- Accept precede Established

Oppure:

- RST senza DisconnectInitiated

→ lifecycle spezzato.

Dove documentarti

- Microsoft Networking Blog:
  - "TCP RACK in Windows"
  - "TCP Fast Open Implementation in Windows"

- Video:
  - "Windows Transport Stack Evolution – NetDev Conference"


🔴 4. PID Reuse

Windows riutilizza PID molto velocemente:

- PID 5320
- process exit
- PID 5320
- new process (entro ms)

Se hai:

- 5-tuple + PID

come flow key:

potresti correlare:

- FIN di processo A

con:

- Connect di processo B

Failure Mode

Database:

- SrcIP = legit app
- DstIP = C2 server
- Duration = 12ms

in realtà:

- due processi diversi.

Mitigazione

Serve aggiungere:

- ProcessStartKey
- CreateTime

ETW provider:

- Microsoft-Windows-Kernel-Process

Dove documentarti

- "Process Lifetime and PID Reuse – Windows Internals"

- ETW Provider:
  - Kernel-Process

- Libro:
  - Windows Internals Part 1 – Processes


🔴 5. Flow Collision

Port reuse + TIME_WAIT recycling:

- 10.0.0.5:50432 → 1.1.1.1:443
- close
- reopen entro 1s stesso 5-tuple

Il TCP stack consente reuse.

ETW vede:

- stesso:
  - LocalIP
  - RemoteIP
  - LocalPort
  - RemotePort
  - PID

Failure Mode

Il tuo flow cache:

- fonde due connessioni

lifetime diventa errato

CloseReason mismatch

Mitigazione

Serve:

- FlowStartTimestamp
- Sequence boundary

Dove documentarti

- RFC:
  - RFC 793 TIME_WAIT
  - RFC 6191 Port Reuse

- Microsoft:
  - "Ephemeral Port Reuse in Windows TCP"


🔴 6. ETW Session Hijacking

ETW è:

- shared kernel facility

Un attacker con:

- SeSystemProfilePrivilege

può:

- stopparla
- ridurre buffer
- creare competing session
- saturare logger

Failure Mode

Il tuo agent:

- continua a ricevere eventi

ma:

- eventi persi selettivamente

Oppure:

- session kill → network invisibility.

Dove documentarti

- "ETW Threat Hunting: Attacking and Defending Logging"

- SpecterOps:
  - "Subverting ETW for Defense Evasion"

- Whitepaper:
  - "Abusing Windows Telemetry"


📌 Implicazione Chiave

Passando da polling a ETW:

stai passando da:

- sampling error

a:

- causal reconstruction risk

Il sistema diventa:

- ✔ più accurato
- ❗ più fragile semanticamente

Serve quindi:

- correlazione robusta
- build-aware parsing
- PID lifetime tracking
- flow aging logic
- ETW health monitoring

Questi rischi sono gestibili, ma solo trattando ETW come:

- high-frequency lossy telemetry stream

non come ground truth deterministico.
