using System.IO;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    public class Attachment
    {
        public Attachment(string contentId, string contentType, Stream stream)
        {
            Stream = stream;
            ContentType = contentType;
            ContentId = contentId;
        }

        public string ContentId { get; set; }

        public Stream Stream { get; set; }

        public string ContentType { get; set; }
    }
}