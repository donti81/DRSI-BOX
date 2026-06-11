namespace DRSIBOX.Models
{
    public class UploadLog
    {
        public long Id { get; set; }
        public string FileName { get; set; } = default!;
        public string OriginalName { get; set; } = default!;
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public string? Folder { get; set; }
    }
}
