namespace DRSIBOX.Services
{
    public interface IEmailService
    {
        Task SendAsync(string subject, string body);
        Task SendAsync(string to, string subject, string body);
    }
}
