using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Starterkit.Models;
using Starterkit.Services;
using System.IO.Compression;
using System.Text.Json;

namespace Starterkit.Pages.Dl
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly IDownloadTokenService _tokens;
        private readonly IUploadNotificationService _notifications;
        private readonly IUploadLogService _uploadLog;
        private readonly IWebHostEnvironment _env;

        public string? ErrorMessage { get; private set; }

        public IndexModel(
            IDownloadTokenService tokens,
            IUploadNotificationService notifications,
            IUploadLogService uploadLog,
            IWebHostEnvironment env)
        {
            _tokens = tokens;
            _notifications = notifications;
            _uploadLog = uploadLog;
            _env = env;
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ErrorMessage = "Povezava je neveljavna.";
                return Page();
            }

            var dt = await _tokens.ValidateAsync(token);
            if (dt is null)
            {
                ErrorMessage = "Povezava je neveljavna ali je potekla.";
                return Page();
            }

            var notif = await _notifications.GetByIdAsync(dt.NotifId);
            if (notif is null || string.IsNullOrWhiteSpace(notif.FilesDetail))
            {
                ErrorMessage = "Datoteke niso na voljo.";
                return Page();
            }

            var entries = JsonSerializer.Deserialize<List<FileDetailDto>>(notif.FilesDetail) ?? [];
            var ids = entries
                .Where(e => e.Id.HasValue)
                .Select(e => e.Id!.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                ErrorMessage = "Datoteke niso na voljo.";
                return Page();
            }

            var files = await _uploadLog.GetByIdsAsync(ids);
            if (files.Count == 0)
            {
                ErrorMessage = "Datoteke niso na voljo.";
                return Page();
            }

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

            return File(ms, "application/zip", $"dokumenti-{dt.NotifId}.zip");
        }

        private string ResolveFilePath(UploadLog file) =>
            string.IsNullOrEmpty(file.Folder)
                ? Path.Combine(_env.WebRootPath, "uploads", file.FileName)
                : Path.Combine(_env.WebRootPath, "uploads", file.Folder, file.FileName);
    }
}
