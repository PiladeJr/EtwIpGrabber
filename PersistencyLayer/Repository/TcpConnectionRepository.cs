using EtwIpGrabber.PersistencyLayer.Filters;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models;
using EtwIpGrabber.TcpLifeCycleReconstruction.Models.Enumerations;
using EtwIpGrabber.TdhParsing.Normalization;
using Microsoft.Data.Sqlite;

namespace EtwIpGrabber.PersistencyLayer.Repository
{
    internal sealed class TcpConnectionRepository : ITcpConnectionRepository
    {
        public async Task UpsertFlowAsync(TcpFlowInstance flow, CancellationToken ct)
        {
            await using var conn = new SqliteConnection(DbConfig.ConnectionString);

            await conn.OpenAsync(ct);

            await using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA synchronous=NORMAL; PRAGMA temp_store=MEMORY;";
                await pragma.ExecuteNonQueryAsync(ct);
            }

            var cmd = conn.CreateCommand();
            var table = TableResolver.FlowTable(flow.Classification);

            cmd.CommandText =
                $"""
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

            cmd.Parameters.AddWithValue("$lip", ConversionUtil.FormatIPv4(flow.Key.LocalIp));
            cmd.Parameters.AddWithValue("$lport", flow.Key.LocalPort);

            cmd.Parameters.AddWithValue("$rip", ConversionUtil.FormatIPv4(flow.Key.RemoteIp));
            cmd.Parameters.AddWithValue("$rport", flow.Key.RemotePort);

            cmd.Parameters.AddWithValue("$first", flow.FirstSeenUtc);
            cmd.Parameters.AddWithValue("$last", flow.LastSeenUtc);

            cmd.Parameters.AddWithValue("$flags", BuildFlags(flow));
            cmd.Parameters.AddWithValue("$state", (int)flow.State);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task UpsertFlowBatchAsync(
            IReadOnlyList<TcpFlowInstance> flows, CancellationToken ct)
        {
            if (flows.Count == 0) return;

            await using var conn = new SqliteConnection(DbConfig.ConnectionString);
            await conn.OpenAsync(ct);

            // PRAGMA di sessione: enforced su questa connessione
            await using (var pragma = conn.CreateCommand())
            {
                pragma.CommandText = "PRAGMA synchronous=NORMAL; PRAGMA temp_store=MEMORY;";
                await pragma.ExecuteNonQueryAsync(ct);
            }

            await using var tx = await conn.BeginTransactionAsync(ct);

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.Transaction = (SqliteTransaction)tx;

                cmd.CommandText =
                    """
                    INSERT INTO $table
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
                        flags     = excluded.flags,
                        state     = excluded.state
                    """;

                // Prepara i parametri una sola volta (riusati per ogni row)
                var pTable = cmd.Parameters.Add("$table", SqliteType.Text);
                var pCid = cmd.Parameters.Add("$cid", SqliteType.Text);
                var pPid = cmd.Parameters.Add("$pid", SqliteType.Integer);
                var pPname = cmd.Parameters.Add("$pname", SqliteType.Text);
                var pLip = cmd.Parameters.Add("$lip", SqliteType.Text);
                var pLport = cmd.Parameters.Add("$lport", SqliteType.Integer);
                var pRip = cmd.Parameters.Add("$rip", SqliteType.Text);
                var pRport = cmd.Parameters.Add("$rport", SqliteType.Integer);
                var pFirst = cmd.Parameters.Add("$first", SqliteType.Text);
                var pLast = cmd.Parameters.Add("$last", SqliteType.Text);
                var pFlags = cmd.Parameters.Add("$flags", SqliteType.Integer);
                var pState = cmd.Parameters.Add("$state", SqliteType.Integer);

                await cmd.PrepareAsync(ct); // compilazione query una sola volta

                foreach (var flow in flows)
                {
                    var table = TableResolver.FlowTable(flow.Classification);
                    pTable.Value = table;
                    pCid.Value = flow.CommunityId;
                    pPid.Value = flow.Key.ProcessId;
                    pPname.Value = flow.ProcessName;
                    pLip.Value = ConversionUtil.FormatIPv4(flow.Key.LocalIp);
                    pLport.Value = flow.Key.LocalPort;
                    pRip.Value = ConversionUtil.FormatIPv4(flow.Key.RemoteIp);
                    pRport.Value = flow.Key.RemotePort;
                    pFirst.Value = flow.FirstSeenUtc;
                    pLast.Value = flow.LastSeenUtc;
                    pFlags.Value = BuildFlags(flow);
                    pState.Value = (int)flow.State;

                    await cmd.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task InsertLifecycleAsync(
            TcpConnectionLifecycle lifecycle,
            CancellationToken ct)
        {
            await using var conn = new SqliteConnection(DbConfig.ConnectionString);
            await conn.OpenAsync(ct);
            
            var table = TableResolver.LifecycleTable(lifecycle.Classification);
            var cmd = conn.CreateCommand();

            cmd.CommandText =
                $"""
                INSERT INTO {table}
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
    }
}
