using System.Collections.Generic;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    public class MailMessage
    {
        public List<string> To { get; } = new List<string>();

        public string From { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public List<Attachment> Attachments { get; } = new List<Attachment>();
    }
}