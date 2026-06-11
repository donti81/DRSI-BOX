namespace DRSIBOX.Models
{
    public class DownloadToken
    {
        public long Id { get; set; }
        public string Token { get; set; } = "";
        public long NotifId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? CreatedBy { get; set; }
    }
}
