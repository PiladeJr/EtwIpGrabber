using Microsoft.Data.Sqlite;

namespace EtwIpGrabber.PersistencyLayer.Repository
{
    internal sealed class DatabaseInitializer
    {
        public static async Task InitializeAsync(CancellationToken ct = default)
        {
            await using var conn = new SqliteConnection(DbConfig.ConnectionString);
            await conn.OpenAsync(ct);

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

            CREATE TABLE IF NOT EXISTS internal_tcp_flows(
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

            CREATE TABLE IF NOT EXISTS internal_tcp_lifecycle(
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

            CREATE TABLE IF NOT EXISTS public_tcp_flows(
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

            CREATE TABLE IF NOT EXISTS public_tcp_lifecycle(
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
                source_table,
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
                weekday,
                hour,

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

            FROM v_all_tcp_lifecycle;
            

            CREATE VIEW IF NOT EXISTS v_all_tcp_lifecycle AS

            SELECT
                'all' AS source_table,
                *
            FROM tcp_lifecycle

            UNION ALL

            SELECT
                'internal' AS source_table,
                *
            FROM internal_tcp_lifecycle

            UNION ALL

            SELECT
                'public' AS source_table,
                *
            FROM public_tcp_lifecycle;
            """;

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
