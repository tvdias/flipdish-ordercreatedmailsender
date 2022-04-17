### About
The application sets up a webhook which listens for order creation messages. Once a message is received on that webhook, it transforms it into an email that includes barcode images for items that have the code defined as metadata.

#### Testing
In your local environment, simply POST the contents of `TestWebhooks\1.json` to `https://{localhost}/api/WebhookReceiver?to=john.doe@mail.com&to=jane.doe@mail.com&currency=eur&metadatakey=eancode&storeId=1234&storeId=12345`.

It's also possible to test messaging byt setting up an Azure Service Bus Queue named `emailnotifications-ordercreated`, adding `ServiceBusConnstring` to local.settings.json and publishing a message to the queue (sample message on `message_sample.json`).

The parameters are as follows:
- `to` - email address where to send the mail (can add multiple)
- `currency` - currency to be displayed in the mail
- `metadatakey` - metadata key for the EAN code (case sensitive)
- `storeid` - store identifiers control which stores orders we want to process (can add multiple)

#### TODO
- Add unit tests to increase code coverage (current code coverage: 2.68%)
- Configure Serilog on app settings file and replace sink
- Review code
- Add external mail service