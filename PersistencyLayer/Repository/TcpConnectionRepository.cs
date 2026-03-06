using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using EtwIpGrabber.TdhParsing.Normalization;
using Microsoft.Data.Sqlite;

namespace EtwIpGrabber.PersistencyLayer.Repository
{
    internal sealed class TcpConnectionRepository : ITcpConnectionRepository
    {
        private readonly string _connectionString = 
            $"Data Source={Path.Combine(AppContext.BaseDirectory, "Connections.db")}";

        public async Task UpsertFlowAsync(TcpFlowInstance flow, CancellationToken ct)
        {
            await using var conn = new SqliteConnection(_connectionString);

            await conn.OpenAsync(ct);
            await EnsureSchemaAsync(conn);

            var cmd = conn.CreateCommand();

            cmd.CommandText =
                """
                INSERT INTO tcp_flows
                (community_id, process_id, process_name,
                 local_ip, local_port,
                 remote_ip, remote_port,
                 first_seen, last_seen,
                 flags, state)
                VALUES
                ($cid,$pid,$pname,
                 $lip,$lport,
                 $rip,$rport,
                 $first,$last,
                 $flags,$state)
                ON CONFLICT(community_id)
                DO UPDATE SET
                    last_seen = excluded.last_seen,
                    flags = excluded.flags,
                    state = excluded.state
                """;

            cmd.Parameters.AddWithValue("$cid", flow.CommunityId);
            cmd.Parameters.AddWithValue("$pid", flow.Key.ProcessId);
            cmd.Parameters.AddWithValue("$pname", flow.ProcessName);

            cmd.Parameters.AddWithValue("$lip", flow.Key.LocalIp);
            cmd.Parameters.AddWithValue("$lport", flow.Key.LocalPort);

            cmd.Parameters.AddWithValue("$rip", flow.Key.RemoteIp);
            cmd.Parameters.AddWithValue("$rport", flow.Key.RemotePort);

            cmd.Parameters.AddWithValue("$first", flow.FirstSeenUtc);
            cmd.Parameters.AddWithValue("$last", flow.LastSeenUtc);

            cmd.Parameters.AddWithValue("$flags", BuildFlags(flow));
            cmd.Parameters.AddWithValue("$state", (int)flow.State);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task InsertLifecycleAsync(
            TcpConnectionLifecycle lifecycle,
            CancellationToken ct)
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);
            await EnsureSchemaAsync(conn);
            var cmd = conn.CreateCommand();

            cmd.CommandText =
                """
                INSERT INTO tcp_lifecycle
                (community_id, process_id, process_name,
                 local_ip, local_port,
                 remote_ip, remote_port,
                 start_at, end_at, duration,
                 weekday, hour,
                 outcome, handshake, direction)
                VALUES
                ($cid,$pid,$pname,
                 $lip,$lport,
                 $rip,$rport,
                 $start,$end,$dur,
                 $weekday,$hour,
                 $outcome,$handshake,$dir)
                """;

            cmd.Parameters.AddWithValue("$cid", lifecycle.CommunityId);
            cmd.Parameters.AddWithValue("$pid", lifecycle.ProcessId);
            cmd.Parameters.AddWithValue("$pname", lifecycle.ProcessName);

            cmd.Parameters.AddWithValue("$lip", ConversionUtil.FormatIPv4(lifecycle.LocalIP));
            cmd.Parameters.AddWithValue("$lport", lifecycle.LocalPort);

            cmd.Parameters.AddWithValue("$rip", ConversionUtil.FormatIPv4(lifecycle.RemoteIP));
            cmd.Parameters.AddWithValue("$rport", lifecycle.RemotePort);

            cmd.Parameters.AddWithValue("$start", lifecycle.StartAt);
            cmd.Parameters.AddWithValue("$end", lifecycle.EndAt);
            cmd.Parameters.AddWithValue("$dur", lifecycle.Duration.TotalMilliseconds);

            cmd.Parameters.AddWithValue("$weekday", (int)lifecycle.EndAt.DayOfWeek);
            cmd.Parameters.AddWithValue("$hour", lifecycle.EndAt.Hour);

            cmd.Parameters.AddWithValue("$outcome", (int)lifecycle.Outcome);
            cmd.Parameters.AddWithValue("$handshake", (int)lifecycle.Handshake);
            cmd.Parameters.AddWithValue("$dir", (int)lifecycle.Direction);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static int BuildFlags(TcpFlowInstance flow)
        {
            TcpFlowFlags flags = TcpFlowFlags.None;

            if (flow.SeenConnect) flags |= TcpFlowFlags.Connect;
            if (flow.SeenAccept) flags |= TcpFlowFlags.Accept;
            if (flow.SeenSend) flags |= TcpFlowFlags.Send;
            if (flow.SeenReceive) flags |= TcpFlowFlags.Receive;
            if (flow.SeenDisconnect) flags |= TcpFlowFlags.Disconnect;
            if (flow.SeenRetransmit) flags |= TcpFlowFlags.Retransmit;
            if (flow.SeenReconnect) flags |= TcpFlowFlags.Reconnect;
            if (flow.SeenFail) flags |= TcpFlowFlags.Fail;

            return (int)flags;
        }

        private async Task EnsureSchemaAsync(SqliteConnection conn)
        {
            var cmd = conn.CreateCommand();

            cmd.CommandText =
            """
                CREATE TABLE IF NOT EXISTS tcp_flows(
                community_id TEXT PRIMARY KEY,
                process_id INTEGER,
                process_name TEXT,
                local_ip TEXT,
                local_port INTEGER,
                remote_ip TEXT,
                remote_port INTEGER,
                first_seen TEXT,
                last_seen TEXT,
                flags INTEGER,
                state INTEGER
                );

                CREATE TABLE IF NOT EXISTS tcp_lifecycle(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    community_id TEXT,
                    process_id INTEGER,
                    process_name TEXT,
                    local_ip TEXT,
                    local_port INTEGER,
                    remote_ip TEXT,
                    remote_port INTEGER,
                    start_at TEXT,
                    end_at TEXT,
                    duration REAL,
                    weekday INTEGER,
                    hour INTEGER,
                    outcome INTEGER,
                    handshake INTEGER,
                    direction INTEGER
                );

                CREATE VIEW IF NOT EXISTS v_tcp_lifecycle_readable AS
                SELECT
                    id,
                    community_id,
                    process_id,
                    process_name,
                    local_ip,
                    local_port,
                    remote_ip,
                    remote_port,
                    start_at,
                    end_at,
                    duration,

                CASE outcome
                    WHEN 0 THEN 'Closed'
                    WHEN 1 THEN 'Refused'
                    WHEN 2 THEN 'Timeout'
                    WHEN 3 THEN 'Established'
                    WHEN 4 THEN 'Aborted'
                    ELSE 'Unknown'
                END AS outcome,

                CASE handshake
                    WHEN 0 THEN 'None'
                    WHEN 1 THEN 'SynSent'
                    WHEN 2 THEN 'Established'
                    WHEN 3 THEN 'Closing'
                    ELSE 'Unknown'
                END AS handshake,

                CASE direction
                    WHEN 0 THEN 'Unknown'
                    WHEN 1 THEN 'Outbound'
                    WHEN 2 THEN 'Inbound'
                    WHEN 3 THEN 'Local'
                    ELSE 'Unknown'
                END AS direction

                FROM tcp_lifecycle;
            """;

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
