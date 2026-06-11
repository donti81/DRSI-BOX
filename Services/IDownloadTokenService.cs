using Starterkit.Models;

namespace Starterkit.Services
{
    public interface IDownloadTokenService
    {
        Task<string> CreateAsync(long notifId, string? createdBy, TimeSpan validity);
        Task<DownloadToken?> ValidateAsync(string token);
    }
}
