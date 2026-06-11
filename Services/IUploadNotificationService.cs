using DRSIBOX.Models;

namespace DRSIBOX.Services
{
    public interface IUploadNotificationService
    {
        Task<long> SaveAsync(UploadNotification notification);
        Task<IList<UploadNotification>> GetAllAsync();
        Task<IList<UploadNotification>> GetBySentByAsync(IList<string> usernames);
        Task<UploadNotification?> GetByIdAsync(long id);
    }
}
