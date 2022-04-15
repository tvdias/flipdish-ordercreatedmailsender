using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Flipdish.Recruiting.WebhookReceiverTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Flipdish.Recruiting.WebhookReceiver.Services.Mailer
{
    internal interface IMailer
    {
        public Task SendMailAsync(MailMessage message);
    }
}