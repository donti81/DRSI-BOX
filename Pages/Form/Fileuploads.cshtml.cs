using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Starterkit.Models;
using Starterkit.Services;
using System.Text.Json;

namespace Starterkit.Pages.Form
{
    public class FileuploadsModel : PageModel
    {
        private readonly IWebHostEnvironment _env;
        private readonly IUploadLogService _uploadLog;
        private readonly IUploadNotificationService _notifications;
        private readonly IEmailService _email;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDownloadTokenService _tokens;
        private readonly IConfiguration _config;

        public bool IsDrsi { get; private set; }
        public string PersonalFolder { get; private set; } = "";

        public FileuploadsModel(IWebHostEnvironment env, IUploadLogService uploadLog, IUploadNotificationService notifications, IEmailService email, UserManager<ApplicationUser> userManager, IDownloadTokenService tokens, IConfiguration config)
        {
            _env = env;
            _uploadLog = uploadLog;
            _notifications = notifications;
            _email = email;
            _userManager = userManager;
            _tokens = tokens;
            _config = config;
        }

        public void OnGet()
        {
            IsDrsi = User.IsInRole("DRSI");
            if (IsDrsi)
                PersonalFolder = User.Identity?.Name ?? "drsi-user";
        }

        public async Task<IActionResult> OnPostUploadAsync()
        {
            var file = Request.Form.Files.FirstOrDefault();

            if (file is null || file.Length == 0)
                return new JsonResult(new { success = false, error = "No file received." });

            var folder = Request.Form["folder"].FirstOrDefault() ?? "";

            if (User.IsInRole("DRSI"))
                folder = User.Identity?.Name ?? "drsi-user";
            else if (string.IsNullOrWhiteSpace(folder) || folder.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return new JsonResult(new { success = false, error = "Izberite veljavno občino." });

            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
            var uploadsDir  = Path.GetFullPath(Path.Combine(uploadsRoot, folder));

            if (!uploadsDir.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
                return new JsonResult(new { success = false, error = "Neveljavna mapa." });

            Directory.CreateDirectory(uploadsDir);

            var safeExt = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{safeExt}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            await _uploadLog.LogAsync(new UploadLog
            {
                FileName     = fileName,
                OriginalName = file.FileName,
                ContentType  = file.ContentType,
                FileSize     = file.Length,
                UploadedAt   = DateTime.UtcNow,
                IpAddress    = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UploadedBy   = User.Identity?.Name,
                Folder       = string.IsNullOrEmpty(folder) ? null : folder,
            });

            return new JsonResult(new { success = true, fileName });
        }

        public async Task<IActionResult> OnPostSendNotificationAsync(
            [FromForm] string? message,
            [FromForm] string? folder,
            [FromForm] int fileCount,
            [FromForm] long totalSize,
            [FromForm] string? filesDetail,
            [FromForm] string? recipients)
        {
            if (fileCount <= 0)
                return new JsonResult(new { success = false, error = "Ni naloženih datotek." });

            // Enrich filesDetail JSON with upload_log IDs
            var enrichedDetail = filesDetail;
            try
            {
                var parsed = JsonSerializer.Deserialize<List<FileDetailDto>>(filesDetail ?? "[]") ?? [];
                var lookback = DateTime.UtcNow.AddHours(-24);
                var enriched = new List<FileDetailDto>();
                foreach (var f in parsed)
                {
                    var log = await _uploadLog.FindRecentByNameAndSizeAsync(f.Name, f.Size, User.Identity?.Name, lookback);
                    enriched.Add(f with { Id = log?.Id });
                }
                enrichedDetail = JsonSerializer.Serialize(enriched);
            }
            catch { }

            var emailValidator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            List<string> parsedRecipients = [];
            if (!string.IsNullOrWhiteSpace(recipients))
            {
                parsedRecipients = recipients
                    .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(e => e.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var invalid = parsedRecipients.Where(e => !emailValidator.IsValid(e)).ToList();
                if (invalid.Count > 0)
                    return new JsonResult(new { success = false, error = "Neveljavni e-poštni naslovi: " + string.Join(", ", invalid) });
            }
            var cleanedRecipients = parsedRecipients.Count > 0 ? string.Join(", ", parsedRecipients) : null;

            var notifId = await _notifications.SaveAsync(new UploadNotification
            {
                Message     = message,
                Folder      = folder,
                FileCount   = fileCount,
                TotalSize   = totalSize,
                FilesDetail = enrichedDetail,
                SentAt      = DateTime.UtcNow,
                SentBy      = User.Identity?.Name,
                IpAddress   = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Recipients  = cleanedRecipients,
            });

            try
            {
                var subject = $"Novo obvestilo o naloženih datotekah – {folder ?? "/"} ({fileCount} dat.)";

                string? downloadLink = null;
                if (User.IsInRole("DRSI") && parsedRecipients.Count > 0)
                {
                    var validityDays = _config.GetValue<int>("DownloadToken:ValidityDays", 7);
                    var token = await _tokens.CreateAsync(notifId, User.Identity?.Name, TimeSpan.FromDays(validityDays));
                    downloadLink = Url.Page("/Dl/Index", pageHandler: null, values: new { token }, protocol: Request.Scheme)!;
                }

                var body = BuildEmailBody(message, folder, fileCount, totalSize, enrichedDetail, User.Identity?.Name, downloadLink);

                if (User.IsInRole("DRSI") && parsedRecipients.Count > 0)
                {
                    foreach (var to in parsedRecipients)
                        await _email.SendAsync(to, subject, body);
                }
                else
                {
                    await _email.SendAsync(subject, body);
                }
            }
            catch { /* e-pošta je neobvezna — ne zavrnemo zahteve ob napaki */ }

            return new JsonResult(new { success = true });
        }
        private static string BuildEmailBody(string? message, string? folder, int fileCount, long totalSize, string? filesDetail, string? sender, string? downloadLink = null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Prejeli ste obvestilo o naloženih datotekah.");
            sb.AppendLine();
            sb.AppendLine($"Pošiljatelj : {sender ?? "—"}");
            sb.AppendLine($"Mapa/občina : {folder ?? "—"}");
            sb.AppendLine($"Število dat.: {fileCount}");
            sb.AppendLine($"Skupna vel. : {FormatSize(totalSize)}");
            if (!string.IsNullOrWhiteSpace(message))
            {
                sb.AppendLine();
                sb.AppendLine("Sporočilo:");
                sb.AppendLine(message);
            }

            try
            {
                var files = JsonSerializer.Deserialize<List<FileDetailDto>>(filesDetail ?? "[]") ?? [];
                if (files.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Datoteke:");
                    foreach (var f in files)
                        sb.AppendLine($"  • {f.Name}  ({FormatSize(f.Size)})");
                }
            }
            catch { }

            if (!string.IsNullOrWhiteSpace(downloadLink))
            {
                sb.AppendLine();
                sb.AppendLine("Prenesite vse datoteke (ZIP, veljavno 7 dni):");
                sb.AppendLine(downloadLink);
            }

            return sb.ToString();
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1_073_741_824) return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)     return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024)         return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}
