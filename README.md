### About
The application sets up a webhook which listens for order creation messages. Once a message is received on that webhook, it transforms it into an email that includes barcode images for items that have the code defined as metadata.

#### Testing
In your local environment, simply POST the contents of `TestWebhooks\1.json` to `https://{localhost}/api/WebhookReceiver?to=john.doe@mail.com&to=jane.doe@mail.com&currency=eur&metadatakey=eancode&storeId=1234&storeId=12345`.

The parameters are as follows:
- `to` - email address where to send the mail (can add multiple)
- `currency` - currency to be displayed in the mail
- `metadatakey` - metadata key for the EAN code (case sensitive)
- `storeid` - store identifiers control which stores orders we want to process (can add multiple)

It's also possible to test messaging byt setting up an Azure Service Bus Queue named `emailnotifications-ordercreated`, adding `ServiceBusConnstring` to local.settings.json and publishing a message to the queue (sample message on `message_sample.json`).

#### TODO
- Add unit tests to increase code coverage (currently under 12%)
- Create interfaces and use IoC whenever possible
- Avoid mixing logic and style
- Move logic on the functions to a service
- Better configure Serilog
...

#### Disclamer
On a realistic cenario, the changes would be created on many different pull requests, following user stories. It would have a QA and/or staging environment and a propper pipeline for CI/CD.

Some changes (code organization, code style, naming, ...) were avoided in order to make it easier to compare the changes on this PR.

I'd not use webhooks in production, in favor of messaging. It's more reliable and have strong benefits (including, but not limited to better decoupling, more resilience, later processing, retries, ...).

PS: It took around 8h to 10h to do these changes.
