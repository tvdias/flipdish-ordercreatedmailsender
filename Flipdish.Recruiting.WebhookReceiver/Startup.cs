using System.IO;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Flipdish.Recruiting.WebhookReceiver.Services;
using Flipdish.Recruiting.WebhookReceiver.Services.Mailer;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Flipdish.Recruiting.WebhookReceiver.Startup))]

namespace Flipdish.Recruiting.WebhookReceiver
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services
                .AddOptions<AppSettings>()
                .Configure<IConfiguration>((settings, configuration) => configuration.GetSection("AppSettings").Bind(settings));

            builder.Services
                .AddSingleton<EmailRenderer>()
                .AddTransient<EmailService>()
                .AddTransient<IMailer, SmtpMailer>()
                .AddSingleton<MapService>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: false, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }
    }
}