using System.Threading.Tasks;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Microsoft.Extensions.Options;
using Mail = System.Net.Mail;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    internal class SmtpMailer : IMailer
    {
        private readonly SmtpSettings _smtpSettings;

        public SmtpMailer(IOptions<SmtpSettings> appSettings)
        {
            _smtpSettings = appSettings.Value;
        }

        public async Task SendMailAsync(MailMessage message)
        {
            var mailMessage = new Mail.MailMessage
            {
                From = new Mail.MailAddress(message.From),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = true
            };

            foreach (var t in message.To)
            {
                mailMessage.To.Add(t);
            }

            foreach (var entry in message.Attachments)
            {
                var attachment = new Mail.Attachment(entry.Stream, entry.ContentType)
                {
                    ContentId = entry.ContentId
                };

                mailMessage.Attachments.Add(attachment);
            }

            using var mailer = new Mail.SmtpClient
            {
                Host = _smtpSettings.Host,
                Port = _smtpSettings.Port,
                Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            await mailer.SendMailAsync(mailMessage);
        }
    }
}