using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flipdish.Recruiting.WebhookReceiver.Models;
using Flipdish.Recruiting.WebhookReceiver.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Flipdish.Recruiting.WebhookReceiver
{
    public class OrderCreatedProcessor
    {
        private readonly EmailRendererService _emailRenderer;
        private readonly EmailService _emailService;
        private readonly ILogger<OrderCreatedProcessor> _log;

        public OrderCreatedProcessor(EmailService emailService, EmailRendererService emailRenderer, ILogger<OrderCreatedProcessor> log)
        {
            _emailService = emailService;
            _emailRenderer = emailRenderer;
            _log = log;
        }

        [FunctionName("MessageReceiver")]
        public async Task MessageReceiver([ServiceBusTrigger("emailnotifications-ordercreated", Connection = "ServiceBusConnstring")] EmailNotificationOrderCreatedEvent orderCreatedEvent)
        {
            _log.LogInformation("C# ServiceBus queue trigger function processed message.");

            await ProcessRequestAsync(orderCreatedEvent);
        }

        [FunctionName("WebhookReceiver")]
        public async Task<IActionResult> WebhookReceiver([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            _log.LogInformation("C# HTTP trigger function processed a request.");

            string test = req.Query["test"];
            var storeIdParams = req.Query["storeId"].ToArray();
            var currencyString = req.Query["currency"].FirstOrDefault();
            var barcodeMetadataKey = req.Query["metadataKey"].First() ?? "eancode";
            var to = req.Query["to"];

            OrderCreatedWebhook orderCreatedWebhook;

            if (req.Method == "POST")
            {
                var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                orderCreatedWebhook = JsonConvert.DeserializeObject<OrderCreatedWebhook>(requestBody);
            }
            else if (!string.IsNullOrEmpty(test))
            {
                var templateFilePath = Path.Combine("TestWebhooks", test);
                var testWebhookJson = new StreamReader(templateFilePath).ReadToEnd();

                orderCreatedWebhook = JsonConvert.DeserializeObject<OrderCreatedWebhook>(testWebhookJson);
            }
            else
            {
                throw new Exception("No body found or test param.");
            }

            var orderId = orderCreatedWebhook.Body.Order.OrderId;
            var storeIds = new List<int>();

            if (storeIdParams.Length > 0)
            {
                foreach (var storeIdString in storeIdParams)
                {
                    if (int.TryParse(storeIdString, out var storeId))
                    {
                        storeIds.Add(storeId);
                    }
                    else
                    {
                        // TODO: storeId = 0 is added in order to keep retro compatibility. Can we remove that?
                        storeIds.Add(0);
                    }
                }

                if (!storeIds.Contains(orderCreatedWebhook.Body.Order.Store.Id.Value))
                {
                    _log.LogInformation($"Skipping order #{orderId}");
                    return new ContentResult { Content = $"Skipping order #{orderId}", ContentType = "text/html" };
                }
            }

            var currency = Currency.EUR;
            if (!string.IsNullOrEmpty(currencyString) && Enum.TryParse(typeof(Currency), currencyString.ToUpper(), out var currencyObject))
            {
                currency = (Currency)currencyObject;
            }

            var orderCreatedEvent = new EmailNotificationOrderCreatedEvent()
            {
                BarcodeMetadataKey = barcodeMetadataKey,
                To = to,
                Currency = currency,
                Content = orderCreatedWebhook,
            };

            var emailOrder = await ProcessRequestAsync(orderCreatedEvent);

            return new ContentResult { Content = emailOrder, ContentType = "text/html" };
        }

        private async Task<string> ProcessRequestAsync(EmailNotificationOrderCreatedEvent orderCreatedEvent)
        {
            var orderId = orderCreatedEvent.Content.Body.Order.OrderId;

            try
            {
                var emailOrder = _emailRenderer.RenderEmailOrder(
                    orderCreatedEvent.Content.Body.Order,
                    orderCreatedEvent.Content.Body.AppId,
                    orderCreatedEvent.BarcodeMetadataKey,
                    orderCreatedEvent.Currency,
                    out var emailImages);

                await _emailService.Send(new[] { orderCreatedEvent.To }, $"New Order #{orderId}", emailOrder, emailImages);

                _log.LogInformation($"Email sent for order #{orderId}.", new { orderCreatedEvent.Content.Body.Order.OrderId });

                return emailOrder;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Error occured during processing order #{orderId}");
                throw;
            }
        }
    }
}