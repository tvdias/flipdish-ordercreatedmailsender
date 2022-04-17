using System.Collections.Generic;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    internal class SendgridMailer : IMailer
    {
        private readonly ISendGridClient _mailer;

        public SendgridMailer(ISendGridClient mailer)
        {
            _mailer = mailer;
        }

        public async Task SendMailAsync(MailMessage message)
        {
            var sendgridMessage = new SendGridMessage
            {
                From = new EmailAddress(message.From),
                Subject = message.Subject,
                HtmlContent = message.Body
            };

            foreach (var t in message.To)
            {
                sendgridMessage.AddTo(t);
            }

            if (message.Attachments != null)
            {
                sendgridMessage.Attachments = new List<SendGrid.Helpers.Mail.Attachment>();

                foreach (var attachment in message.Attachments)
                {
                    await sendgridMessage.AddAttachmentAsync(
                        filename: attachment.ContentId,
                        contentStream: attachment.Stream,
                        type: attachment.ContentType,
                        disposition: "inline",
                        content_id: attachment.ContentId);
                }
            }

            await _mailer.SendEmailAsync(sendgridMessage);
        }
    }
}