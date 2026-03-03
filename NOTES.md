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

Feature da aggiungere in futuro:
[ ] aggiunta del provider kernel-based ATS per avere più visibilità su connessioni TCP
[ ] aggiungere l'implementazione per provider UDP
[ ] ordinare gli eventi tcp ottenuti da ETW in modo da ricostruire correttamente le connessioni TCP (es. correlare Connect, Accept, FIN, Abort) per una stessa connessione (il modello attuale consuma eventi per ordine di ricezione. più veloce per il buffer ma crea confusione nell'interpretazione dei dati)

