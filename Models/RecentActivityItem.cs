namespace Starterkit.Models
{
    public class RecentActivityItem
    {
        public string EventType { get; set; } = default!;
        public long FileId { get; set; }
        public string OriginalName { get; set; } = default!;
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime EventAt { get; set; }
        public string? EventBy { get; set; }
    }
}
