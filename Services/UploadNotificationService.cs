using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Starterkit.Models;

namespace Starterkit.Services
{
    public class UploadNotificationService : IUploadNotificationService
    {
        private readonly string _connectionString;

        public UploadNotificationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleLogs")
                ?? throw new InvalidOperationException("Connection string 'OracleLogs' is not configured.");
        }

        public async Task<long> SaveAsync(UploadNotification n)
        {
            const string sql = """
                INSERT INTO upload_notifications
                    (message, folder, file_count, total_size, files_detail, sent_at, sent_by, ip_address, poslano)
                VALUES
                    (:message, :folder, :fileCount, :totalSize, :filesDetail, :sentAt, :sentBy, :ipAddress, :poslano)
                RETURNING id INTO :outId
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("message",     OracleDbType.Varchar2, 4000) { Value = (object?)n.Message     ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("folder",      OracleDbType.Varchar2, 100)  { Value = (object?)n.Folder      ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("fileCount",   OracleDbType.Int32)           { Value = n.FileCount });
            cmd.Parameters.Add(new OracleParameter("totalSize",   OracleDbType.Int64)           { Value = n.TotalSize });
            cmd.Parameters.Add(new OracleParameter("filesDetail", OracleDbType.Clob)            { Value = (object?)n.FilesDetail ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("sentAt",      OracleDbType.TimeStamp)       { Value = (OracleTimeStamp)n.SentAt });
            cmd.Parameters.Add(new OracleParameter("sentBy",      OracleDbType.Varchar2, 256)   { Value = (object?)n.SentBy     ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("ipAddress",   OracleDbType.Varchar2, 50)    { Value = (object?)n.IpAddress  ?? DBNull.Value });
            cmd.Parameters.Add(new OracleParameter("poslano",     OracleDbType.Varchar2, 4000)  { Value = (object?)n.Recipients ?? DBNull.Value });
            var outId = new OracleParameter("outId", OracleDbType.Int64) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outId);
            await cmd.ExecuteNonQueryAsync();
            return Convert.ToInt64(((OracleDecimal)outId.Value).ToInt64());
        }

        public async Task<UploadNotification?> GetByIdAsync(long id)
        {
            const string sql = """
                SELECT id, message, folder, file_count, total_size, files_detail, sent_at, sent_by, ip_address, poslano
                FROM upload_notifications
                WHERE id = :id
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("id", OracleDbType.Int64) { Value = id });
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return MapRow(reader);
        }

        public async Task<IList<UploadNotification>> GetAllAsync()
        {
            const string sql = """
                SELECT id, message, folder, file_count, total_size, files_detail, sent_at, sent_by, ip_address, poslano
                FROM upload_notifications
                ORDER BY sent_at DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadNotification>();
            while (await reader.ReadAsync())
                list.Add(MapRow(reader));
            return list;
        }

        public async Task<IList<UploadNotification>> GetBySentByAsync(IList<string> usernames)
        {
            if (usernames.Count == 0) return [];
            var paramNames = usernames.Select((_, i) => $":u{i}").ToList();
            var inClause = string.Join(",", paramNames);
            var sql = $"""
                SELECT id, message, folder, file_count, total_size, files_detail, sent_at, sent_by, ip_address, poslano
                FROM upload_notifications
                WHERE sent_by IN ({inClause})
                ORDER BY sent_at DESC
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            for (int i = 0; i < usernames.Count; i++)
                cmd.Parameters.Add(new OracleParameter($"u{i}", OracleDbType.Varchar2, 256) { Value = usernames[i] });
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<UploadNotification>();
            while (await reader.ReadAsync())
                list.Add(MapRow(reader));
            return list;
        }

        private static UploadNotification MapRow(System.Data.Common.DbDataReader reader) => new()
        {
            Id          = reader.GetInt64(0),
            Message     = reader.IsDBNull(1) ? null : reader.GetString(1),
            Folder      = reader.IsDBNull(2) ? null : reader.GetString(2),
            FileCount   = reader.GetInt32(3),
            TotalSize   = reader.GetInt64(4),
            FilesDetail = reader.IsDBNull(5) ? null : reader.GetString(5),
            SentAt      = reader.GetDateTime(6),
            SentBy      = reader.IsDBNull(7) ? null : reader.GetString(7),
            IpAddress   = reader.IsDBNull(8) ? null : reader.GetString(8),
            Recipients  = reader.IsDBNull(9) ? null : reader.GetString(9),
        };
    }
}
