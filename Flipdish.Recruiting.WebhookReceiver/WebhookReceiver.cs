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
    public class WebhookReceiver
    {
        private readonly EmailService _emailService;
        private readonly EmailRenderer _emailRenderer;
        private readonly ILogger<WebhookReceiver> _log;

        public WebhookReceiver(EmailService emailService, EmailRenderer emailRenderer, ILogger<WebhookReceiver> log)
        {
            _emailService = emailService;
            _emailRenderer = emailRenderer;
            _log = log;
        }

        [FunctionName("WebhookReceiver")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            int? orderId = null;

            try
            {
                _log.LogInformation("C# HTTP trigger function processed a request.");

                OrderCreatedWebhook orderCreatedWebhook;

                string test = req.Query["test"];
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

                var orderCreatedEvent = orderCreatedWebhook.Body;

                orderId = orderCreatedEvent.Order.OrderId;
                var storeIds = new List<int>();
                var storeIdParams = req.Query["storeId"].ToArray();
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

                    if (!storeIds.Contains(orderCreatedEvent.Order.Store.Id.Value))
                    {
                        _log.LogInformation($"Skipping order #{orderId}");
                        return new ContentResult { Content = $"Skipping order #{orderId}", ContentType = "text/html" };
                    }
                }

                var currency = Currency.EUR;
                var currencyString = req.Query["currency"].FirstOrDefault();
                if (!string.IsNullOrEmpty(currencyString) && Enum.TryParse(typeof(Currency), currencyString.ToUpper(), out var currencyObject))
                {
                    currency = (Currency)currencyObject;
                }

                var barcodeMetadataKey = req.Query["metadataKey"].First() ?? "eancode";

                var emailOrder = _emailRenderer.RenderEmailOrder(orderCreatedEvent.Order, orderCreatedEvent.AppId, barcodeMetadataKey, currency);

                try
                {
                    await _emailService.Send(req.Query["to"], $"New Order #{orderId}", emailOrder, _emailRenderer._imagesWithNames);
                }
                catch (Exception ex)
                {
                    _log.LogError($"Error occured during sending email for order #{orderId}" + ex);
                }

                _log.LogInformation($"Email sent for order #{orderId}.", new { orderCreatedEvent.Order.OrderId });

                return new ContentResult { Content = emailOrder, ContentType = "text/html" };
            }
            catch (Exception ex)
            {
                _log.LogError(ex, $"Error occured during processing order #{orderId}");
                throw;
            }
        }
    }
}