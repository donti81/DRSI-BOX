using Starterkit.Models;

namespace Starterkit.Services
{
    public interface IUploadLogService
    {
        Task LogAsync(UploadLog log);
        Task<IList<UploadLog>> GetAllAsync();
        Task<IList<UploadLog>> GetByUploadersAsync(IList<string> usernames);
        Task<UploadLog?> GetByIdAsync(long id);
        Task<IList<UploadLog>> GetDeletedAsync();
        Task<IList<UploadLog>> GetDeletedByUploadersAsync(IList<string> usernames);
        Task SoftDeleteAsync(long id, string? deletedBy);
        Task<string?> DeleteAsync(long id);
        Task<IList<RecentActivityItem>> GetRecentActivityAsync(int limit = 50);
        Task<IList<RecentActivityItem>> GetRecentActivityByUploadersAsync(IList<string> usernames, int limit = 50);
        Task<UploadLog?> FindRecentByNameAndSizeAsync(string originalName, long fileSize, string? uploadedBy, DateTime after);
        Task<IList<UploadLog>> GetByIdsAsync(IEnumerable<long> ids);
    }
}
