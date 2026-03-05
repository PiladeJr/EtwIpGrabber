using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using Microsoft.Data.Sqlite;

namespace EtwIpGrabber.PersistencyLayer.Repository
{
    internal sealed class TcpConnectionRepository(string connectionString)
                : ITcpConnectionRepository
    {
        private readonly string _connectionString = connectionString;

        public async Task UpsertFlowAsync(TcpFlowInstance flow, CancellationToken ct)
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);

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

            var cmd = conn.CreateCommand();

            cmd.CommandText =
                """
                INSERT INTO tcp_lifecycle
                (community_id, process_id, process_name,
                 local_ip, local_port,
                 remote_ip, remote_port,
                 start_at, end_at, duration,
                 outcome, handshake, direction)
                VALUES
                ($cid,$pid,$pname,
                 $lip,$lport,
                 $rip,$rport,
                 $start,$end,$dur,
                 $outcome,$handshake,$dir)
                """;

            cmd.Parameters.AddWithValue("$cid", lifecycle.CommunityId);
            cmd.Parameters.AddWithValue("$pid", lifecycle.ProcessId);
            cmd.Parameters.AddWithValue("$pname", lifecycle.ProcessName);

            cmd.Parameters.AddWithValue("$lip", lifecycle.LocalIP);
            cmd.Parameters.AddWithValue("$lport", lifecycle.LocalPort);

            cmd.Parameters.AddWithValue("$rip", lifecycle.RemoteIP);
            cmd.Parameters.AddWithValue("$rport", lifecycle.RemotePort);

            cmd.Parameters.AddWithValue("$start", lifecycle.StartAt);
            cmd.Parameters.AddWithValue("$end", lifecycle.EndAt);
            cmd.Parameters.AddWithValue("$dur", lifecycle.Duration.TotalMilliseconds);

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
    }
}
