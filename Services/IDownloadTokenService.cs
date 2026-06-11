using DRSIBOX.Models;

namespace DRSIBOX.Services
{
    public interface IDownloadTokenService
    {
        Task<string> CreateAsync(long notifId, string? createdBy, TimeSpan validity);
        Task<DownloadToken?> ValidateAsync(string token);
    }
}
