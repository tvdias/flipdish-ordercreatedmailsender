using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Flipdish.Recruiting.WebhookReceiver.Services;
using Flipdish.Recruiting.WebhookReceiver.Services.Mailer;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Flipdish.Recruiting.WebhookReceiverTests
{
    public class EmailServiceTests : BaseTest
    {
        private readonly Mock<IMailer> mailerMock;
        private readonly IOptions<AppSettings> appSettings;
        private readonly EmailService emailService;

        public EmailServiceTests()
        {
            mailerMock = new Mock<IMailer>();
            appSettings = Options.Create(fixture.Create<AppSettings>());
            emailService = new EmailService(mailerMock.Object, appSettings);
        }

        [Fact]
        public async Task Send_AllRightNoCc_Success()
        {
            // Arrange
            var to = new List<string>() { "to@email.com" };
            var subject = fixture.Create<string>();
            var body = fixture.Create<string>();
            var attachements = new Dictionary<string, System.IO.Stream>();

            mailerMock
                .Setup(m => m.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.FromResult(true));

            // Act
            await emailService.Send(to, subject, body, attachements);

            // Assert
            mailerMock.Verify(m => m.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task Send_AllRightWithCc_Success()
        {
            // Arrange
            var to = new List<string>() { "to@email.com" };
            var subject = fixture.Create<string>();
            var body = fixture.Create<string>();
            var attachements = new Dictionary<string, System.IO.Stream>();
            var cc = new List<string>() { "tocc@email.com" };

            mailerMock
                .Setup(m => m.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.FromResult(true));

            // Act
            await emailService.Send(to, subject, body, attachements, cc);

            // Assert
            mailerMock.Verify(m => m.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }
    }
}