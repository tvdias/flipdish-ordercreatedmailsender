using System.IO;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    public class Attachment
    {
        /// <summary>
        /// Initializes a new instance of the System.Net.Mail.Attachment class with the specified stream and name.
        /// </summary>
        /// <param name="stream">A readable System.IO.Stream that contains the content for this attachment.</param>
        /// <param name="contentType">A System.String that contains the value for the System.Net.Mime.ContentType.Name
        /// property of the System.Net.Mime.ContentType associated with this attachment. This value can be null.</param>
        public Attachment(Stream stream, string contentType)
        {
            Stream = stream;
            ContentType = contentType;
            ContentId = contentType;
        }

        public string ContentId { get; set; }

        public Stream Stream { get; set; }

        public string ContentType { get; set; }
    }
}