# EtwIpGrabber

Tool per monitorare eventi TCP/IP dal kernel Windows via ETW e ricostruire lifecycle di connessioni.

## Caratteristiche principali

- Worker service .NET 8 che sottoscrive il provider kernel `NetworkTCPIP`.
- Parsing robusto degli eventi (Connect/Accept/Send/Recv/Disconnect).
- Note operative e rischi raccolti in `NOTES.md` (lossy telemetry, schema changes, PID reuse, ecc.).

## Architecture

Mappa delle componenti: elenco dei "package" (cartelle/namespace) che hai creato nel progetto e che per ora non contengono ancora file di implementazione. Qui rappresentiamo solo queste componenti, come richiesto.

- `EtwStructure/SessionManager` — gestione delle sessioni ETW (creazione, riavvio, limitazioni buffer).
- `EtwStructure/ProviderConfiguration` — configurazioni e metadati dei provider ETW (manifests, mapping campi, versioning).
- `EtwStructure/RealTimeConsumer` — consumer real-time che elabora eventi per ricostruzione lifecycle e output immediato.
- `EtwStructure/EventDispatcher` — dispatcher centrale per inoltrare eventi ETW ai consumer interni.
- `EtwStructure/Metrics&Health` — raccolta metriche, health checks e monitoring della sessione ETW.

Prerequisiti

- Windows (ETW kernel provider)
- .NET 8 SDK
- Eseguire con privilegi Administrator (ETW session richiede elevazione)

Build & esecuzione

1. Costruire il progetto:

   dotnet build

Uso

Troubleshooting

- Se non vedi output markdown in anteprima per `NOTES.md`, assicurati che il file abbia estensione `.md` (già presente) e che l'editor supporti la preview (VS Code: `Ctrl+Shift+V`).
- Verifica i privilegi: `TraceEventSession.IsElevated()` deve restituire true.
- Controlla che non esistano sessioni ETW concorrenti che possono interferire.