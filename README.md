cose da mettere nel readme:

# Overview:
	il progetto è un servizio che utilizza ETW per monitorare le connessioni TCP in tempo reale.
	deve essere integrato con un sistema esistente di analisi delle connessioni e sostituire un meccanismo di parsing di netscan.

# Struttura

	il progetto è diviso in 4 moduli + moduli di utilità: 
		- etw_implementation: controller, provider injection, session manager e consumer real time.
		- tdh_parsing: recupero dei metadati, definizione di una struttura per il provider, parsing sequenziale e normalizzazione dei dati.
		- lifecycle reconstruction: logica di ricostruzione di una connessione con meccanismi di 
		 definizione degli eventi di inizio e fine di una connessione 
		 (se in corso, chiusa, negata, timed out,...). stato dell'handshake TCP alla fine del ciclo vita 
		 direzione della connessione (inbound o outbound), durata della connessione, ecc.
		- db: contiene la logica per la gestione del database SQLite, inclusa la creazione del database e delle tabelle, e l'inserimento dei dati e visualizzazioni custom
		- service di utilità:
			- generazione community ID di una connessione da 5 tuple (src ip, src port, dst ip, dst port, protocol)
			- resolver del process name dal suo id.
			- classificazione di una connessione (interne, esterne, loopback,...)
			- filtri per db di persistenza dei dati: di default vengono salvati solo connessioni private. è possibile configurare il filtro per avere connessioni pubbliche, loopback, tutte le connessioni, ecc.
# Logica di funzionamento del servizio:
	dopo aver spiegato come è strutturato il progetto, spiega la logica di funzionamento del servizio,
	ripercorrendo i passaggi principali che avvengono dall'avvio del servizio:
		etw startup -> emissione di eventi -> parsing dei dati -> ricostruzione del ciclo di connessione e salvataggio in db in parallelo.
		per ogni fase , spiega brevemente cosa accade, cosa cambia durante le varie fasi e cosa ottengo in output. senza entrare troppo nei dettagli. 
# Richiami alla documentazione presente per ogni fase.
	non spiegare ogni singolo punto delle varie fasi nello specifico. il funzionamento di ogni step 
	principale è spiegato in dettaglio nei file presenti nella cartella docs. 
	è possibile consultare la documentazione per ogni fase per avere una spiegazione più dettagliata di ogni step.
# Query di visualizzazione:
	di' che è stata aggiunta una query per visualizzare i dati salvati da int a text, in modo
	da renderli più facili da interpretare da un punto di vista semantico.
	la query è presente nell'initialize del db e viene creata solo se non esiste già

# Estensioni future:
	- mostrare un esempio di come estendere il servizio a più provider. in particolar modo mostrare un esempio effettivo di come aggiungere UDP provider
	- gli eventi vengono salvati in un buffer concorrente. alcuni eventi potrebbero essere emessi prima di altri. 
	risultato, potrei vedere eventi accept o close prima di un connect. dipende dall'ordine in cui
	etw riceve ed emette gli eventi. non è un bug e ci sono meccanismi per la ricostruzione dei cicli di connessione.
	magari occorre implementare un meccanismo di ordinamento degli eventi emessi dal buffer per rendere la
	visualizzazione più coerente e di più facile interpretazione.
	- integrare un sistema di visualizzazione dei dati in colonna che sostituisca i log di eventi windows.
	magari una visualizzazione da terminale ogni volta che viene chiamato il comando del servizio. (aggiungere un comando custom).


