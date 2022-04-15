using System.Threading.Tasks;
using Mail = System.Net.Mail;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    internal class SmtpMailer : IMailer
    {
        public async Task SendMailAsync(MailMessage message)
        {
            var mailMessage = new Mail.MailMessage
            {
                From = new Mail.MailAddress(message.From),
                Subject = message.Subject,
                Body = message.Body
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
                Host = "localhost",
                Port = 1025,
                Credentials = new System.Net.NetworkCredential("", "")
            };

            await mailer.SendMailAsync(mailMessage);
        }
    }
}