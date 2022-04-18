using System.Threading.Tasks;

namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    public interface IMailer
    {
        public Task SendMailAsync(MailMessage message);
    }
}