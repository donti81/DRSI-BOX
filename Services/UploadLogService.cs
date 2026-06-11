using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using DRSIBOX.Models;

namespace DRSIBOX.Services
{
    public class UploadLogService : IUploadLogService
    {
        private readonly string _connectionString;

        public UploadLogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleLogs")
                ?? throw new InvalidOperationException("Connection string 'OracleLogs' is not configured.");
        }

        public async Task<IList<UploadLog>> GetAllAsync()
        {
            const string sql = """
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE deleted_at IS NULL
                ORDER BY uploaded_at DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadLog>();
            while (await reader.ReadAsync())
                list.Add(MapRow(reader));
            return list;
        }

        public async Task<IList<UploadLog>> GetByUploadersAsync(IList<string> usernames)
        {
            if (usernames.Count == 0) return [];
            var inClause = BuildUsernameInParams(usernames, out var parameters);
            var sql = $"""
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE deleted_at IS NULL
                  AND uploaded_by IN ({inClause})
                ORDER BY uploaded_at DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadLog>();
            while (await reader.ReadAsync())
                list.Add(MapRow(reader));
            return list;
        }

        public async Task<UploadLog?> GetByIdAsync(long id)
        {
            const string sql = """
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE id = :id
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("id", OracleDbType.Int64) { Value = id });
            await using var reader = await cmd.ExecuteReaderAsync();

            return await reader.ReadAsync() ? MapRow(reader) : null;
        }

        public async Task<IList<UploadLog>> GetDeletedAsync()
        {
            const string sql = """
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE deleted_at IS NOT NULL
                ORDER BY deleted_at DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadLog>();
            while (await reader.ReadAsync())
                list.Add(MapRow(reader));
            return list;
        }

        public async Task<IList<UploadLog>> GetDeletedByUploadersAsync(IList<string> usernames)
        {
            if (usernames.Count == 0) return [];
            var inClause = BuildUsernameInParams(usernames, out var parameters);
            var sql = $"""
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE deleted_at IS NOT NULL
                  AND uploaded_by IN ({inClause})
                ORDER BY deleted_at DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadLog>();
            while (await reader.ReadAsync())
                list.Add(MapRow(reader));
            return list;
        }

        public async Task SoftDeleteAsync(long id, string? deletedBy)
        {
            const string sql = """
                UPDATE upload_logs
                SET deleted_at = :deletedAt, deleted_by = :deletedBy
                WHERE id = :id
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("deletedAt", OracleDbType.TimeStamp)      { Value = (OracleTimeStamp)DateTime.UtcNow });
            cmd.Parameters.Add(new OracleParameter("deletedBy", OracleDbType.Varchar2, 256)  { Value = (object?)deletedBy ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("id",        OracleDbType.Int64)          { Value = id });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<string?> DeleteAsync(long id)
        {
            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            await using var selectCmd = new OracleCommand(
                "SELECT file_name FROM upload_logs WHERE id = :id", conn);
            selectCmd.Parameters.Add(new OracleParameter("id", OracleDbType.Int64) { Value = id });
            var fileName = (await selectCmd.ExecuteScalarAsync()) as string;

            if (fileName is null) return null;

            await using var deleteCmd = new OracleCommand(
                "DELETE FROM upload_logs WHERE id = :id", conn);
            deleteCmd.Parameters.Add(new OracleParameter("id", OracleDbType.Int64) { Value = id });
            await deleteCmd.ExecuteNonQueryAsync();

            return fileName;
        }

        public async Task LogAsync(UploadLog log)
        {
            const string sql = """
                INSERT INTO upload_logs (file_name, original_name, content_type, file_size, uploaded_at, ip_address, uploaded_by, folder)
                VALUES (:fileName, :originalName, :contentType, :fileSize, :uploadedAt, :ipAddress, :uploadedBy, :folder)
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();

            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("fileName",    OracleDbType.Varchar2, 500) { Value = log.FileName });
            cmd.Parameters.Add(new OracleParameter("originalName", OracleDbType.Varchar2, 500) { Value = log.OriginalName });
            cmd.Parameters.Add(new OracleParameter("contentType", OracleDbType.Varchar2, 200) { Value = (object?)log.ContentType ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("fileSize",    OracleDbType.Int64)          { Value = log.FileSize });
            cmd.Parameters.Add(new OracleParameter("uploadedAt",  OracleDbType.TimeStamp)      { Value = (OracleTimeStamp)log.UploadedAt });
            cmd.Parameters.Add(new OracleParameter("ipAddress",   OracleDbType.Varchar2, 50)   { Value = (object?)log.IpAddress ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("uploadedBy",  OracleDbType.Varchar2, 256)  { Value = (object?)log.UploadedBy ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("folder",      OracleDbType.Varchar2, 100)  { Value = (object?)log.Folder ?? DBNull.Value });

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IList<RecentActivityItem>> GetRecentActivityByUploadersAsync(IList<string> usernames, int limit = 50)
        {
            if (usernames.Count == 0) return [];

            // Oracle ne podpira ponovne uporabe istih parametrov v različnih delih UNION,
            // zato vsak UNION blok dobi lasten set parametrov z unikatnimi imeni.
            var inClause1 = BuildUsernameInParams(usernames, out var params1, "ra1");
            var inClause2 = BuildUsernameInParams(usernames, out var params2, "ra2");
            var inClause3 = BuildUsernameInParams(usernames, out var params3, "ra3");

            var sql = $"""
                SELECT event_type, file_id, original_name, content_type, file_size, event_at, event_by
                FROM (
                    SELECT 'Uploaded'   AS event_type, id AS file_id, original_name, content_type, file_size,
                           uploaded_at  AS event_at, uploaded_by AS event_by
                    FROM upload_logs
                    WHERE deleted_at IS NULL AND uploaded_by IN ({inClause1})
                    UNION ALL
                    SELECT 'Deleted'    AS event_type, id AS file_id, original_name, content_type, file_size,
                           deleted_at   AS event_at, deleted_by AS event_by
                    FROM upload_logs
                    WHERE deleted_at IS NOT NULL AND uploaded_by IN ({inClause2})
                    UNION ALL
                    SELECT 'Downloaded' AS event_type, u.id AS file_id, u.original_name, u.content_type, u.file_size,
                           d.downloaded_at AS event_at, d.downloaded_by AS event_by
                    FROM download_logs d
                    JOIN upload_logs u ON u.id = d.upload_log_id
                    WHERE u.uploaded_by IN ({inClause3})
                )
                ORDER BY event_at DESC
                FETCH FIRST :limit ROWS ONLY
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.AddRange(params1);
            cmd.Parameters.AddRange(params2);
            cmd.Parameters.AddRange(params3);
            cmd.Parameters.Add(new OracleParameter("limit", OracleDbType.Int32) { Value = limit });
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<RecentActivityItem>();
            while (await reader.ReadAsync())
            {
                list.Add(new RecentActivityItem
                {
                    EventType    = reader.GetString(0),
                    FileId       = reader.GetInt64(1),
                    OriginalName = reader.GetString(2),
                    ContentType  = reader.IsDBNull(3) ? null : reader.GetString(3),
                    FileSize     = reader.GetInt64(4),
                    EventAt      = reader.GetDateTime(5),
                    EventBy      = reader.IsDBNull(6) ? null : reader.GetString(6),
                });
            }
            return list;
        }

        public async Task<IList<RecentActivityItem>> GetRecentActivityAsync(int limit = 50)
        {
            const string sql = """
                SELECT event_type, file_id, original_name, content_type, file_size, event_at, event_by
                FROM (
                    SELECT 'Uploaded'   AS event_type, id AS file_id, original_name, content_type, file_size,
                           uploaded_at  AS event_at, uploaded_by AS event_by
                    FROM upload_logs
                    WHERE deleted_at IS NULL
                    UNION ALL
                    SELECT 'Deleted'    AS event_type, id AS file_id, original_name, content_type, file_size,
                           deleted_at   AS event_at, deleted_by AS event_by
                    FROM upload_logs
                    WHERE deleted_at IS NOT NULL
                    UNION ALL
                    SELECT 'Downloaded' AS event_type, u.id AS file_id, u.original_name, u.content_type, u.file_size,
                           d.downloaded_at AS event_at, d.downloaded_by AS event_by
                    FROM download_logs d
                    JOIN upload_logs u ON u.id = d.upload_log_id
                )
                ORDER BY event_at DESC
                FETCH FIRST :limit ROWS ONLY
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("limit", OracleDbType.Int32) { Value = limit });
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<RecentActivityItem>();
            while (await reader.ReadAsync())
            {
                list.Add(new RecentActivityItem
                {
                    EventType    = reader.GetString(0),
                    FileId       = reader.GetInt64(1),
                    OriginalName = reader.GetString(2),
                    ContentType  = reader.IsDBNull(3) ? null : reader.GetString(3),
                    FileSize     = reader.GetInt64(4),
                    EventAt      = reader.GetDateTime(5),
                    EventBy      = reader.IsDBNull(6) ? null : reader.GetString(6),
                });
            }
            return list;
        }

        public async Task<UploadLog?> FindRecentByNameAndSizeAsync(string originalName, long fileSize, string? uploadedBy, DateTime after)
        {
            const string sql = """
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE original_name = :originalName
                  AND file_size     = :fileSize
                  AND uploaded_at  >= :after
                  AND deleted_at   IS NULL
                  AND (uploaded_by = :uploadedBy OR (:uploadedBy IS NULL AND uploaded_by IS NULL))
                ORDER BY uploaded_at DESC
                FETCH FIRST 1 ROWS ONLY
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("originalName", OracleDbType.Varchar2, 500) { Value = originalName });
            cmd.Parameters.Add(new OracleParameter("fileSize",     OracleDbType.Int64)          { Value = fileSize });
            cmd.Parameters.Add(new OracleParameter("after",        OracleDbType.TimeStamp)       { Value = (OracleTimeStamp)after });
            cmd.Parameters.Add(new OracleParameter("uploadedBy",   OracleDbType.Varchar2, 256)   { Value = (object?)uploadedBy ?? DBNull.Value });
            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapRow(reader) : null;
        }

        public async Task<IList<UploadLog>> GetByIdsAsync(IEnumerable<long> ids)
        {
            var idList = ids.ToList();
            if (idList.Count == 0) return [];

            var paramNames = idList.Select((_, i) => $":p{i}").ToList();
            var sql = $"""
                SELECT id, file_name, original_name, content_type, file_size,
                       uploaded_at, ip_address, uploaded_by, deleted_at, deleted_by, folder
                FROM upload_logs
                WHERE id IN ({string.Join(",", paramNames)})
                  AND deleted_at IS NULL
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            for (int i = 0; i < idList.Count; i++)
                cmd.Parameters.Add(new OracleParameter($"p{i}", OracleDbType.Int64) { Value = idList[i] });
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadLog>();
            while (await reader.ReadAsync()) list.Add(MapRow(reader));
            return list;
        }

        private static UploadLog MapRow(System.Data.Common.DbDataReader r) => new()
        {
            Id           = r.GetInt64(0),
            FileName     = r.GetString(1),
            OriginalName = r.GetString(2),
            ContentType  = r.IsDBNull(3) ? null : r.GetString(3),
            FileSize     = r.GetInt64(4),
            UploadedAt   = r.GetDateTime(5),
            IpAddress    = r.IsDBNull(6) ? null : r.GetString(6),
            UploadedBy   = r.IsDBNull(7) ? null : r.GetString(7),
            DeletedAt    = r.IsDBNull(8) ? null : r.GetDateTime(8),
            DeletedBy    = r.IsDBNull(9) ? null : r.GetString(9),
            Folder       = r.IsDBNull(10) ? null : r.GetString(10),
        };

        private static string BuildUsernameInParams(IList<string> usernames, out OracleParameter[] parameters, string prefix = "u")
        {
            var paramNames = usernames.Select((_, i) => $":{prefix}{i}").ToList();
            parameters = usernames.Select((name, i) =>
                new OracleParameter($"{prefix}{i}", OracleDbType.Varchar2, 256) { Value = name }
            ).ToArray();
            return string.Join(",", paramNames);
        }
    }
}
