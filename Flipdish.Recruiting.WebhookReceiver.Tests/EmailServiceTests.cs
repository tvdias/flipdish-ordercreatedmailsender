using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Flipdish.Recruiting.WebhookReceiver.Services;
using Flipdish.Recruiting.WebhookReceiver.Services.Mailer;
using Moq;
using Xunit;

namespace Flipdish.Recruiting.WebhookReceiverTests
{
    public class EmailServiceTests : BaseTest
    {
        private Mock<IMailer> mailerMock;
        private readonly EmailService emailService;

        public EmailServiceTests()
        {
            mailerMock = new Mock<IMailer>();
            emailService = new EmailService(mailerMock.Object);
        }

        [Fact]
        public async Task Send_AllRightNoCc_Success()
        {
            // Arrange
            const string from = "from@email.com";
            var to = new List<string>() { "to@email.com" };
            var subject = fixture.Create<string>();
            var body = fixture.Create<string>();
            Dictionary<string, System.IO.Stream> attachements = new Dictionary<string, System.IO.Stream>();

            mailerMock
                .Setup(m => m.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.FromResult(true));

            // Act
            await emailService.Send(from, to, subject, body, attachements);

            // Assert
            mailerMock.Verify(m => m.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }

        [Fact]
        public async Task Send_AllRightWithCc_Success()
        {
            // Arrange
            const string from = "from@email.com";
            var to = new List<string>() { "to@email.com" };
            var subject = fixture.Create<string>();
            var body = fixture.Create<string>();
            Dictionary<string, System.IO.Stream> attachements = new Dictionary<string, System.IO.Stream>();
            var cc = new List<string>() { "tocc@email.com" };

            mailerMock
                .Setup(m => m.SendMailAsync(It.IsAny<MailMessage>()))
                .Returns(Task.FromResult(true));

            // Act
            await emailService.Send(from, to, subject, body, attachements, cc);

            // Assert
            mailerMock.Verify(m => m.SendMailAsync(It.IsAny<MailMessage>()), Times.Once);
        }
    }
}