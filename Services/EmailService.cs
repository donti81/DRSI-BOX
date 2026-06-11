using System.Net;
using System.Net.Mail;

namespace Starterkit.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _from;
        private readonly string _to;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _host   = config["Smtp:Host"]!;
            _port   = int.Parse(config["Smtp:Port"]!);
            _from   = config["Smtp:From"]!;
            _to     = config["Smtp:To"]!;
            _logger = logger;
        }

        public Task SendAsync(string subject, string body) =>
            SendAsync(_to, subject, body);

        public async Task SendAsync(string to, string subject, string body)
        {
            using var client = new SmtpClient(_host)
            {
                EnableSsl             = false,
                UseDefaultCredentials = true,
                //Credentials           = null,
                DeliveryMethod        = SmtpDeliveryMethod.Network,
            };

            using var mail = new MailMessage(_from, to, subject, body)
            {
                IsBodyHtml = false,
            };

            try
            {
                await client.SendMailAsync(mail);
                _logger.LogInformation("E-pošta poslana: {Subject} -> {To}", subject, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Napaka pri pošiljanju e-pošte: {Subject}", subject);
                throw;
            }
        }
    }
}
