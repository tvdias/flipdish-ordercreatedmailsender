using System;
using System.IO;
using System.Net.Http;
using Flipdish.Recruiting.WebhookReceiver.Config;
using Flipdish.Recruiting.WebhookReceiver.Services;
using Flipdish.Recruiting.WebhookReceiver.Services.Mailer;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using SendGrid;
using Serilog;
using Serilog.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Flipdish.Recruiting.WebhookReceiver.Startup))]

namespace Flipdish.Recruiting.WebhookReceiver
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var useSeq = builder.GetContext().Configuration.GetValue<bool>("AppSettings:UseSeq");

            var loggerConfig = new LoggerConfiguration();

            if (useSeq)
            {
                loggerConfig = loggerConfig.WriteTo.Seq("http://localhost:5341");
            }
            else
            {
                loggerConfig = loggerConfig.WriteTo.Console();
            }

            var logger = loggerConfig.CreateLogger();

            builder.Services
                .AddLogging(lb => lb.AddSerilog(logger))
                .AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(logger, false));

            builder.Services
                .AddOptions<AppSettings>()
                .Configure<IConfiguration>((settings, configuration) => configuration.GetSection("AppSettings").Bind(settings));
            builder.Services
                .AddOptions<SmtpSettings>()
                .Configure<IConfiguration>((settings, configuration) => configuration.GetSection("SmtpSettings").Bind(settings));

            SetMailingService(builder);

            builder.Services
                .AddSingleton<EmailRendererService>()
                .AddTransient<EmailService>()
                .AddSingleton<IMapService, MapService>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: false, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }

        private static void SetMailingService(IFunctionsHostBuilder builder)
        {
            var sendgridApiKey = builder.GetContext().Configuration.GetValue<string>("AppSettings:SendgridApiKey");

            if (string.IsNullOrWhiteSpace(sendgridApiKey))
            {
                builder.Services.AddTransient<IMailer, SmtpMailer>();
                return;
            }

            builder.Services
                .AddHttpClient<SendGridClient>("SendGrid")
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(100)));

            builder.Services.AddSingleton<ISendGridClient>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("SendGrid");
                return new SendGridClient(httpClient, sendgridApiKey);
            });

            builder.Services.AddSingleton<IMailer, SendgridMailer>();
        }
    }
}