using Starterkit.Models;

namespace Starterkit.Services
{
    public interface IDownloadLogService
    {
        Task LogAsync(long uploadLogId, string? ipAddress, string? downloadedBy);
        Task LogBatchAsync(IList<long> uploadLogIds, string? ipAddress, string? downloadedBy);
        Task<Dictionary<long, int>> GetCountsAsync();
        Task<Dictionary<long, int>> GetCountsByUploadersAsync(IList<string> usernames);
        Task<IList<DownloadedFileView>> GetDownloadedFilesWithCountsAsync();
        Task<IList<DownloadedFileView>> GetDownloadedFilesWithCountsByUploadersAsync(IList<string> usernames);
    }
}
