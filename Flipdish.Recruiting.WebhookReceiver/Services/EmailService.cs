using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Flipdish.Recruiting.WebhookReceiver.Services.Mailer;
using Microsoft.Extensions.Options;

namespace Flipdish.Recruiting.WebhookReceiver.Services
{
    public class EmailService
    {
        private readonly IMailer _mailer;
        private readonly AppSettings _appSettings;

        public EmailService(IMailer mailer, IOptions<AppSettings> appSettings)
        {
            _mailer = mailer;
            _appSettings = appSettings.Value;
        }

        public async Task Send(IEnumerable<string> to, string subject, string body, Dictionary<string, Stream> attachements, IEnumerable<string> cc = null)
        {
            var mailMessage = new MailMessage
            {
                From = _appSettings.MailSender,
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