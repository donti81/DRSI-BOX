namespace Starterkit.Models
{
    public class UploadNotification
    {
        public long Id { get; set; }
        public string? Message { get; set; }
        public string? Folder { get; set; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public string? FilesDetail { get; set; }
        public DateTime SentAt { get; set; }
        public string? SentBy { get; set; }
        public string? IpAddress { get; set; }
        public string? Recipients { get; set; }
    }
}
