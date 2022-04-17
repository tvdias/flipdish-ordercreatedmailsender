namespace Flipdish.Recruiting.WebhookReceiver.Models
{
    public class EmailNotificationOrderCreatedEvent
    {
        public OrderCreatedWebhook Content { get; set; }

        public string BarcodeMetadataKey { get; set; }

        public string To { get; set; }

        public Currency Currency { get; set; }
    }
}