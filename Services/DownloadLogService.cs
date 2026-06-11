using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using DRSIBOX.Models;

namespace DRSIBOX.Services
{
    public class DownloadLogService : IDownloadLogService
    {
        private readonly string _connectionString;

        public DownloadLogService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleLogs")
                ?? throw new InvalidOperationException("Connection string 'OracleLogs' is not configured.");
        }

        public async Task LogAsync(long uploadLogId, string? ipAddress, string? downloadedBy)
        {
            const string sql = """
                INSERT INTO download_logs (upload_log_id, downloaded_at, ip_address, downloaded_by)
                VALUES (:uploadLogId, :downloadedAt, :ipAddress, :downloadedBy)
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("uploadLogId",   OracleDbType.Int64)        { Value = uploadLogId });
            cmd.Parameters.Add(new OracleParameter("downloadedAt",  OracleDbType.TimeStamp)    { Value = (OracleTimeStamp)DateTime.UtcNow });
            cmd.Parameters.Add(new OracleParameter("ipAddress",     OracleDbType.Varchar2, 50)  { Value = (object?)ipAddress ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("downloadedBy",  OracleDbType.Varchar2, 256) { Value = (object?)downloadedBy ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task LogBatchAsync(IList<long> uploadLogIds, string? ipAddress, string? downloadedBy)
        {
            if (uploadLogIds.Count == 0) return;
            const string sql = """
                INSERT INTO download_logs (upload_log_id, downloaded_at, ip_address, downloaded_by)
                VALUES (:uploadLogId, :downloadedAt, :ipAddress, :downloadedBy)
                """;
            var now = (OracleTimeStamp)DateTime.UtcNow;
            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            foreach (var id in uploadLogIds)
            {
                await using var cmd = new OracleCommand(sql, conn);
                cmd.Parameters.Add(new OracleParameter("uploadLogId",  OracleDbType.Int64)         { Value = id });
                cmd.Parameters.Add(new OracleParameter("downloadedAt", OracleDbType.TimeStamp)      { Value = now });
                cmd.Parameters.Add(new OracleParameter("ipAddress",    OracleDbType.Varchar2, 50)   { Value = (object?)ipAddress ?? DBNull.Value });
                cmd.Parameters.Add(new OracleParameter("downloadedBy", OracleDbType.Varchar2, 256)  { Value = (object?)downloadedBy ?? DBNull.Value });
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<Dictionary<long, int>> GetCountsAsync()
        {
            const string sql = """
                SELECT upload_log_id, COUNT(*) AS cnt
                FROM download_logs
                GROUP BY upload_log_id
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var dict = new Dictionary<long, int>();
            while (await reader.ReadAsync())
                dict[reader.GetInt64(0)] = reader.GetInt32(1);

            return dict;
        }

        public async Task<Dictionary<long, int>> GetCountsByUploadersAsync(IList<string> usernames)
        {
            if (usernames.Count == 0) return [];
            var inClause = BuildUsernameInParams(usernames, out var parameters);
            var sql = $"""
                SELECT d.upload_log_id, COUNT(*) AS cnt
                FROM download_logs d
                JOIN upload_logs u ON u.id = d.upload_log_id
                WHERE u.uploaded_by IN ({inClause})
                GROUP BY d.upload_log_id
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();

            var dict = new Dictionary<long, int>();
            while (await reader.ReadAsync())
                dict[reader.GetInt64(0)] = reader.GetInt32(1);

            return dict;
        }

        public async Task<IList<DownloadedFileView>> GetDownloadedFilesWithCountsAsync()
        {
            const string sql = """
                SELECT u.id, u.file_name, u.original_name, u.content_type, u.file_size, u.uploaded_by,
                       COUNT(d.id) AS download_count,
                       MAX(d.downloaded_at) AS last_downloaded_at,
                       MAX(d.downloaded_by) KEEP (DENSE_RANK LAST ORDER BY d.downloaded_at) AS last_downloaded_by
                FROM upload_logs u
                INNER JOIN download_logs d ON d.upload_log_id = u.id
                GROUP BY u.id, u.file_name, u.original_name, u.content_type, u.file_size, u.uploaded_by
                ORDER BY MAX(d.downloaded_at) DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<DownloadedFileView>();
            while (await reader.ReadAsync())
            {
                list.Add(new DownloadedFileView
                {
                    UploadLogId      = reader.GetInt64(0),
                    FileName         = reader.GetString(1),
                    OriginalName     = reader.GetString(2),
                    ContentType      = reader.IsDBNull(3) ? null : reader.GetString(3),
                    FileSize         = reader.GetInt64(4),
                    UploadedBy       = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DownloadCount    = reader.GetInt32(6),
                    LastDownloadedAt = reader.GetDateTime(7),
                    LastDownloadedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                });
            }
            return list;
        }

        public async Task<IList<DownloadedFileView>> GetDownloadedFilesWithCountsByUploadersAsync(IList<string> usernames)
        {
            if (usernames.Count == 0) return [];
            var inClause = BuildUsernameInParams(usernames, out var parameters);
            var sql = $"""
                SELECT u.id, u.file_name, u.original_name, u.content_type, u.file_size, u.uploaded_by,
                       COUNT(d.id) AS download_count,
                       MAX(d.downloaded_at) AS last_downloaded_at,
                       MAX(d.downloaded_by) KEEP (DENSE_RANK LAST ORDER BY d.downloaded_at) AS last_downloaded_by
                FROM upload_logs u
                INNER JOIN download_logs d ON d.upload_log_id = u.id
                WHERE u.uploaded_by IN ({inClause})
                GROUP BY u.id, u.file_name, u.original_name, u.content_type, u.file_size, u.uploaded_by
                ORDER BY MAX(d.downloaded_at) DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<DownloadedFileView>();
            while (await reader.ReadAsync())
            {
                list.Add(new DownloadedFileView
                {
                    UploadLogId      = reader.GetInt64(0),
                    FileName         = reader.GetString(1),
                    OriginalName     = reader.GetString(2),
                    ContentType      = reader.IsDBNull(3) ? null : reader.GetString(3),
                    FileSize         = reader.GetInt64(4),
                    UploadedBy       = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DownloadCount    = reader.GetInt32(6),
                    LastDownloadedAt = reader.GetDateTime(7),
                    LastDownloadedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                });
            }
            return list;
        }

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
