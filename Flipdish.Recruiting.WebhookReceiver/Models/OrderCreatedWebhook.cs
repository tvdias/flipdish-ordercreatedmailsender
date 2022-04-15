using System;

namespace Flipdish.Recruiting.WebhookReceiver.Models
{
    public class OrderCreatedWebhook
    {
        public string Type { get; set; }

        public DateTime CreateTime { get; set; }
        public OrderCreatedEvent Body { get; set; }
    }
}