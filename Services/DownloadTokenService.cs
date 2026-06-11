using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Starterkit.Models;
using System.Security.Cryptography;

namespace Starterkit.Services
{
    public class DownloadTokenService : IDownloadTokenService
    {
        private readonly string _connectionString;

        public DownloadTokenService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OracleLogs")
                ?? throw new InvalidOperationException("Connection string 'OracleLogs' is not configured.");
        }

        public async Task<string> CreateAsync(long notifId, string? createdBy, TimeSpan validity)
        {
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var now = DateTime.UtcNow;
            var expires = now.Add(validity);

            const string sql = """
                INSERT INTO download_tokens (token, notif_id, created_at, expires_at, created_by)
                VALUES (:token, :notifId, :createdAt, :expiresAt, :createdBy)
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("token",     OracleDbType.Varchar2, 64)  { Value = token });
            cmd.Parameters.Add(new OracleParameter("notifId",   OracleDbType.Int64)          { Value = notifId });
            cmd.Parameters.Add(new OracleParameter("createdAt", OracleDbType.TimeStamp)       { Value = (OracleTimeStamp)now });
            cmd.Parameters.Add(new OracleParameter("expiresAt", OracleDbType.TimeStamp)       { Value = (OracleTimeStamp)expires });
            cmd.Parameters.Add(new OracleParameter("createdBy", OracleDbType.Varchar2, 256)  { Value = (object?)createdBy ?? DBNull.Value });
            await cmd.ExecuteNonQueryAsync();

            return token;
        }

        public async Task<DownloadToken?> ValidateAsync(string token)
        {
            const string sql = """
                SELECT id, token, notif_id, created_at, expires_at, created_by
                FROM download_tokens
                WHERE token = :token AND expires_at > SYSTIMESTAMP
                """;

            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new OracleCommand(sql, conn);
            cmd.Parameters.Add(new OracleParameter("token", OracleDbType.Varchar2, 64) { Value = token });
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new DownloadToken
            {
                Id        = reader.GetInt64(0),
                Token     = reader.GetString(1),
                NotifId   = reader.GetInt64(2),
                CreatedAt = reader.GetDateTime(3),
                ExpiresAt = reader.GetDateTime(4),
                CreatedBy = reader.IsDBNull(5) ? null : reader.GetString(5),
            };
        }
    }
}
