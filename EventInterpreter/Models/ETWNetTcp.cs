using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace EtwIpGrabber.EventInterpreter.Models
{
    [SupportedOSPlatform("windows")]
    public sealed class EtwConnectionMonitorClean : BackgroundService
    {
        private readonly ILogger<EtwConnectionMonitorClean> _logger;
        private readonly string _sessionName = "LECS-KernelTcpIp";
        private TraceEventSession? _session;
        private volatile HashSet<IPAddress> _localIps = new();
        private readonly ConcurrentDictionary<string, FlowStats> _flows = new();

        public EtwConnectionMonitorClean(ILogger<EtwConnectionMonitorClean> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 ETW Kernel TcpIp Connection Monitor avviato");
            return Task.Run(() => RunEtwLoop(stoppingToken), stoppingToken);
        }

        private void RefreshLocalIps()
        {
            var set = new HashSet<IPAddress>();

            // loopback
            set.Add(IPAddress.Loopback);
            set.Add(IPAddress.IPv6Loopback);

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    var ip = ua.Address;
                    if (ip == null) continue;

                    // puoi decidere se includere IPv6. Per ora includiamolo.
                    set.Add(ip);
                }
            }

            _localIps = set;
        }

        private bool IsLocalIp(IPAddress ip) => _localIps.Contains(ip);

        private static HashSet<IPAddress> GetLocalIpSet()
        {
            var set = new HashSet<IPAddress>
    {
        IPAddress.Loopback,
        IPAddress.IPv6Loopback
    };

            foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    set.Add(ua.Address);
                }
            }

            return set;
        }

        private void RunEtwLoop(CancellationToken token)
        {
            if (!(TraceEventSession.IsElevated() ?? false))
            {
                _logger.LogError("❌ ETW richiede privilegi Administrator");
                return;
            }

            try
            {
                TraceEventSession.GetActiveSession(_sessionName)?.Dispose();
            }
            catch { }

            using var session = new TraceEventSession(_sessionName);
            _session = session;
            session.StopOnDispose = true;

            session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

            _logger.LogInformation("✅ ETW Kernel TcpIp session avviata (solo IP privati)");

            // ===========================
            // 1️⃣ RACCOLTA IP LOCALI
            // ===========================
            var localIps = GetLocalIpSet();

            var source = session.Source;
            var kernel = new KernelTraceEventParser(source);

            // ===========================
            // 2️⃣ CONNECT = tentativo
            // ===========================
            kernel.TcpIpConnect += data =>
            {
                if (token.IsCancellationRequested) return;

                // filtro: solo IPv4 privati
                if (!IsPrivateIp(data.saddr) || !IsPrivateIp(data.daddr)) return;

                bool fromLocal = localIps.Contains(data.saddr);
                string mark = fromLocal ? " 🚩LOCALHOST ATTEMPT" : "";

                _logger.LogInformation(
                    "🔌 CONNECT{Mark} {Src}:{SrcP} ➜ {Dst}:{DstP} | PID={Pid} ({Proc})",
                    mark,
                    data.saddr, data.sport,
                    data.daddr, data.dport,
                    data.ProcessID,
                    data.ProcessName
                );
            };

            // ===========================
            // 3️⃣ ACCEPT = inbound
            // ===========================
            kernel.TcpIpAccept += data =>
            {
                if (token.IsCancellationRequested) return;

                if (!IsPrivateIp(data.saddr) || !IsPrivateIp(data.daddr)) return;

                _logger.LogInformation(
                    "✅ ACCEPT {Remote}:{RemoteP} ➜ {Local}:{LocalP} | PID={Pid} ({Proc})",
                    data.daddr, data.dport,   // remote
                    data.saddr, data.sport,   // local
                    data.ProcessID,
                    data.ProcessName
                );
            };

            // ===========================
            // 4️⃣ DISCONNECT
            // ===========================
            kernel.TcpIpDisconnect += data =>
            {
                if (token.IsCancellationRequested) return;

                if (!IsPrivateIp(data.saddr) || !IsPrivateIp(data.daddr)) return;

                _logger.LogInformation(
                    "⛔ DISCONNECT {Src}:{SrcP} ↯ {Dst}:{DstP} | PID={Pid} ({Proc})",
                    data.saddr, data.sport,
                    data.daddr, data.dport,
                    data.ProcessID,
                    data.ProcessName
                );
            };

            // ===========================
            // 5️⃣ SEND = traffico uscente (solo se parte dal tuo host)
            // ===========================
            kernel.TcpIpSend += data =>
            {
                if (token.IsCancellationRequested) return;

                if (!IsPrivateIp(data.saddr) || !IsPrivateIp(data.daddr)) return;

                // mostra solo traffico originato dal tuo host
                if (!localIps.Contains(data.saddr)) return;

                _logger.LogInformation(
                    "📤 SEND {Src}:{SrcP} ➜ {Dst}:{DstP} | PID={Pid} ({Proc})",
                    data.saddr, data.sport,
                    data.daddr, data.dport,
                    data.ProcessID,
                    data.ProcessName
                );
            };

            // ===========================
            // 6️⃣ RECV = traffico entrante (solo se arriva al tuo host)
            // ===========================
            kernel.TcpIpRecv += data =>
            {
                if (token.IsCancellationRequested) return;

                if (!IsPrivateIp(data.saddr) || !IsPrivateIp(data.daddr)) return;

                // mostra solo traffico diretto al tuo host
                if (!localIps.Contains(data.daddr)) return;

                _logger.LogInformation(
                    "📥 RECV {Src}:{SrcP} ➜ {Dst}:{DstP} | PID={Pid} ({Proc})",
                    data.saddr, data.sport,
                    data.daddr, data.dport,
                    data.ProcessID,
                    data.ProcessName
                );
            };

            _logger.LogInformation("🎧 In ascolto eventi TcpIp (LAN-only)...");

            source.Process();
        }



        private bool TryParseConnectionEvent(TraceEvent e, out ConnectionInfo conn)
        {
            conn = new ConnectionInfo();

            try
            {
                // Usa il NOME evento, che da PerfView sappiamo essere:
                // Connect / Accept / Disconnect / Recv / Send
                var name = e.EventName?.ToLowerInvariant() ?? string.Empty;

                if (name.Contains("connect"))
                    conn.Type = "CONNECT";
                else if (name.Contains("accept"))
                    conn.Type = "ACCEPT";
                else if (name.Contains("disconnect"))
                    conn.Type = "DISCONNECT";
                else if (name.Contains("recv"))
                    conn.Type = "RECV";
                else if (name.Contains("send"))
                    conn.Type = "SEND";
                else
                    return false; // non è uno degli eventi che ci interessa

                // Kernel TcpIp: saddr/sport (source) e daddr/dport (dest)
                var localAddr = ExtractAddress(e, "saddr", "src", "source");
                var remoteAddr = ExtractAddress(e, "daddr", "dst", "dest");

                if (localAddr == null || remoteAddr == null)
                    return false;

                conn.LocalAddress = localAddr;
                conn.RemoteAddress = remoteAddr;

                conn.LocalPort = ExtractPort(e, "sport", "sourcePort");
                conn.RemotePort = ExtractPort(e, "dport", "destPort");

                return true;
            }
            catch
            {
                return false;
            }
        }

        private IPAddress? ExtractAddress(TraceEvent e, params string[] fieldNames)
        {
            foreach (var name in fieldNames)
            {
                try
                {
                    var idx = Array.IndexOf(e.PayloadNames, name);
                    if (idx < 0)
                        continue;

                    var value = e.PayloadValue(idx);
                    if (value == null)
                        continue;

                    // 1) già IPAddress
                    if (value is IPAddress ip)
                        return ip;

                    // 2) stringa "192.168.7.126"
                    if (value is string s && IPAddress.TryParse(s, out var parsed))
                        return parsed;

                    // 3) uint / int: NON invertire i byte
                    if (value is uint u)
                    {
                        // ETW ci dà il valore giusto, IPAddress(u) lo interpreta correttamente
                        return new IPAddress(u);
                    }

                    if (value is int i && i > 0)
                    {
                        return new IPAddress((uint)i);
                    }

                    // 4) byte[] (4 o 16 byte)
                    if (value is byte[] b && (b.Length == 4 || b.Length == 16))
                        return new IPAddress(b);
                }
                catch
                {
                    // ignora e prova il prossimo nome
                }
            }

            return null;
        }


        private ushort ExtractPort(TraceEvent e, params string[] fieldNames)
        {
            foreach (var name in fieldNames)
            {
                try
                {
                    var idx = Array.IndexOf(e.PayloadNames, name);
                    if (idx < 0)
                        continue;

                    var value = e.PayloadValue(idx);

                    switch (value)
                    {
                        case ushort us:
                            return us;
                        case int i when i >= 0 && i <= 65535:
                            return (ushort)i;
                        case uint ui when ui <= 65535:
                            return (ushort)ui;
                        case string s:
                            // PerfView ti fa vedere sport="38,030": togliamo separatori
                            s = s.Replace(".", "").Replace(",", "");
                            if (ushort.TryParse(s, out var parsed))
                                return parsed;
                            break;
                    }
                }
                catch { }
            }

            return 0;
        }

        private static bool IsPrivateIp(IPAddress ip)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork)
                return false;

            var bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10)
                return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            return false;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("⏹️ Stopping ETW Kernel TcpIp session");

            try
            {
                _session?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore stop ETW");
            }

            await base.StopAsync(cancellationToken);
        }

        private static string Ep(IPAddress ip, ushort port) => $"{ip}:{port}";

        private static string FlowKey(IPAddress a1, ushort p1, IPAddress a2, ushort p2)
        {
            var left = Ep(a1, p1);
            var right = Ep(a2, p2);
            return string.CompareOrdinal(left, right) <= 0 ? $"{left} <-> {right}" : $"{right} <-> {left}";
        }

        private class ConnectionInfo
        {
            public string Type { get; set; } = "";
            public IPAddress LocalAddress { get; set; } = IPAddress.None;
            public ushort LocalPort { get; set; }
            public IPAddress RemoteAddress { get; set; } = IPAddress.None;
            public ushort RemotePort { get; set; }
        }
        private sealed class FlowStats
        {
            public long TxPkts;
            public long RxPkts;
            public long TxBytes;
            public long RxBytes;
            public DateTime LastSeenUtc;
            public int LastPid;
            public string? LastProc;
            public bool InvolvesLocal; // se uno dei due endpoint è IP locale
        }

        private sealed class EtwConnState
        {
            public required IPEndPoint Local;
            public required IPEndPoint Remote;
            public int Pid;
            public string? ProcessName;
            public DateTime LastSeenUtc;
            public bool Established;   // true dopo CONNECT/ACCEPT, false dopo DISCONNECT/TTL
        }
    }
}