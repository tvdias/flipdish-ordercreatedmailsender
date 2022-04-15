using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Flipdish.Recruiting.WebhookReceiver.Services.Mailer;

namespace Flipdish.Recruiting.WebhookReceiver.Services
{
    internal class EmailService
    {
        private readonly IMailer _mailer;

        public EmailService(IMailer mailer)
        {
            _mailer = mailer;
        }

        public async Task Send(string from, IEnumerable<string> to, string subject, string body, Dictionary<string, Stream> attachements, IEnumerable<string> cc = null)
        {
            var mailMessage = new MailMessage
            {
                From = from,
                Subject = subject,
                Body = body
            };

            mailMessage.To.AddRange(to);

            if (cc != null)
            {
                mailMessage.To.AddRange(cc);
            }

            foreach (var nameAndStreamPair in attachements)
            {
                var attachment = new Attachment(nameAndStreamPair.Value, nameAndStreamPair.Key)
                {
                    ContentId = nameAndStreamPair.Key
                };

                mailMessage.Attachments.Add(attachment);
            }

            await _mailer.SendMailAsync(mailMessage);
        }
    }
}