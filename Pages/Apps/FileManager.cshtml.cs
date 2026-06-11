using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DRSIBOX.Models;
using DRSIBOX.Services;
using System.IO.Compression;
using System.Security.Claims;
using System.Text.Json;

namespace DRSIBOX.Pages.Apps
{
    public class FileManagerModel : PageModel
    {
        private readonly IUploadLogService _uploadLog;
        private readonly IDownloadLogService _downloadLog;
        private readonly IUploadNotificationService _notifications;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        [BindProperty(SupportsGet = true)]
        public string? View { get; set; }

        public IList<UploadLog> Files { get; private set; } = [];
        public Dictionary<long, int> DownloadCounts { get; private set; } = [];
        public IList<DownloadedFileView> DownloadedFiles { get; private set; } = [];
        public IList<UploadLog> DeletedFiles { get; private set; } = [];
        public IList<RecentActivityItem> RecentActivity { get; private set; } = [];
        public IList<UploadNotification> Notifications { get; private set; } = [];

        public FileManagerModel(
            IUploadLogService uploadLog,
            IDownloadLogService downloadLog,
            IUploadNotificationService notifications,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager)
        {
            _uploadLog = uploadLog;
            _downloadLog = downloadLog;
            _notifications = notifications;
            _env = env;
            _userManager = userManager;
        }

        public async Task OnGetAsync()
        {
            var usernames = await GetCompanyUsernamesAsync();

            if (View == "downloads")
            {
                DownloadedFiles = usernames is null
                    ? await _downloadLog.GetDownloadedFilesWithCountsAsync()
                    : await _downloadLog.GetDownloadedFilesWithCountsByUploadersAsync(usernames);
            }
            else if (View == "trash")
            {
                DeletedFiles = usernames is null
                    ? await _uploadLog.GetDeletedAsync()
                    : await _uploadLog.GetDeletedByUploadersAsync(usernames);
            }
            else if (View == "recent")
            {
                RecentActivity = usernames is null
                    ? await _uploadLog.GetRecentActivityAsync()
                    : await _uploadLog.GetRecentActivityByUploadersAsync(usernames);
            }
            else if (View == "notifications")
            {
                Notifications = usernames is null
                    ? await _notifications.GetAllAsync()
                    : await _notifications.GetBySentByAsync(usernames);
            }
            else
            {
                if (usernames is null)
                {
                    Files = await _uploadLog.GetAllAsync();
                    DownloadCounts = await _downloadLog.GetCountsAsync();
                }
                else
                {
                    Files = await _uploadLog.GetByUploadersAsync(usernames);
                    DownloadCounts = await _downloadLog.GetCountsByUploadersAsync(usernames);
                }
            }
        }

        private string ResolveFilePath(UploadLog file) =>
            string.IsNullOrEmpty(file.Folder)
                ? Path.Combine(_env.WebRootPath, "uploads", file.FileName)
                : Path.Combine(_env.WebRootPath, "uploads", file.Folder, file.FileName);

        public async Task<IActionResult> OnGetDownloadAsync(long id)
        {
            var file = await _uploadLog.GetByIdAsync(id);
            if (file is null) return NotFound();

            if (!await CanAccessFileAsync(file)) return Forbid();

            var path = ResolveFilePath(file);
            if (!System.IO.File.Exists(path)) return NotFound();

            await _downloadLog.LogAsync(id, HttpContext.Connection.RemoteIpAddress?.ToString(), User.Identity?.Name);

            return PhysicalFile(path, file.ContentType ?? "application/octet-stream", file.OriginalName);
        }

        public async Task<IActionResult> OnGetDownloadAllAsync(long notifId)
        {
            var notif = await _notifications.GetByIdAsync(notifId);
            if (notif is null || string.IsNullOrWhiteSpace(notif.FilesDetail))
                return NotFound();

            if (!await CanAccessNotificationAsync(notif)) return Forbid();

            var entries = JsonSerializer.Deserialize<List<FileDetailDto>>(notif.FilesDetail) ?? [];
            var ids = entries
                .Where(e => e.Id.HasValue)
                .Select(e => e.Id!.Value)
                .Distinct()
                .ToList();
            if (ids.Count == 0) return NotFound();

            var files = await _uploadLog.GetByIdsAsync(ids);
            if (files.Count == 0) return NotFound();

            var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var file in files)
                {
                    var path = ResolveFilePath(file);
                    if (!System.IO.File.Exists(path)) continue;
                    var entry = archive.CreateEntry(file.OriginalName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await using var fs = System.IO.File.OpenRead(path);
                    await fs.CopyToAsync(entryStream);
                }
            }
            ms.Position = 0;

            var loggedIds = files.Where(f => System.IO.File.Exists(ResolveFilePath(f)))
                                 .Select(f => f.Id)
                                 .ToList();
            await _downloadLog.LogBatchAsync(loggedIds,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                User.Identity?.Name);

            return File(ms, "application/zip", $"notification-{notifId}.zip");
        }

        public async Task<IActionResult> OnPostDeleteAsync(long id)
        {
            var file = await _uploadLog.GetByIdAsync(id);
            if (file is null) return RedirectToPage();

            if (!await CanAccessFileAsync(file)) return Forbid();

            await _uploadLog.SoftDeleteAsync(id, User.Identity?.Name);

            var path = Path.Combine(_env.WebRootPath, "uploads", file.FileName);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            return RedirectToPage();
        }

        // Returns null for Admin (no filter), own username for DRSI, or company usernames for User.
        private async Task<IList<string>?> GetCompanyUsernamesAsync()
        {
            if (User.IsInRole("Admin")) return null;
            if (User.IsInRole("DRSI")) return [User.Identity!.Name!];

            var companyIdStr = User.FindFirstValue("CompanyId");
            if (int.TryParse(companyIdStr, out var companyId))
            {
                var users = await _userManager.Users
                    .Where(u => u.CompanyId == companyId)
                    .Select(u => u.UserName!)
                    .ToListAsync();
                return users.Count > 0 ? users : [User.Identity!.Name!];
            }

            return [User.Identity?.Name ?? ""];
        }

        private async Task<bool> CanAccessFileAsync(UploadLog file)
        {
            if (User.IsInRole("Admin")) return true;
            var usernames = await GetCompanyUsernamesAsync();
            return usernames is not null && usernames.Contains(file.UploadedBy ?? "");
        }

        private async Task<bool> CanAccessNotificationAsync(UploadNotification notif)
        {
            if (User.IsInRole("Admin")) return true;
            var usernames = await GetCompanyUsernamesAsync();
            return usernames is not null && usernames.Contains(notif.SentBy ?? "");
        }
    }
}
