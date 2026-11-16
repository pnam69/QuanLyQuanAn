using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace QuanLyQuanAn.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _settings;

        public SmtpEmailSender(
            IOptions<SmtpSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(
            string email,
            string subject,
            string message)
        {
            using var mail = new MailMessage();

            mail.From = new MailAddress(
                _settings.SenderEmail,
                _settings.SenderName);

            mail.To.Add(email);
            mail.Subject = subject;
            mail.Body = message;
            mail.IsBodyHtml = true;

            using var client = new SmtpClient(
                _settings.Host,
                _settings.Port);

            client.Credentials = new NetworkCredential(
                _settings.Username,
                _settings.Password);

            client.EnableSsl = _settings.EnableSsl;

            await client.SendMailAsync(mail);
        }
    }
}