namespace Starterkit.Models
{
    public class DownloadLog
    {
        public long Id { get; set; }
        public long UploadLogId { get; set; }
        public DateTime DownloadedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? DownloadedBy { get; set; }
    }

    public class DownloadedFileView
    {
        public long UploadLogId { get; set; }
        public string FileName { get; set; } = default!;
        public string OriginalName { get; set; } = default!;
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public string? UploadedBy { get; set; }
        public int DownloadCount { get; set; }
        public DateTime LastDownloadedAt { get; set; }
        public string? LastDownloadedBy { get; set; }
    }
}
