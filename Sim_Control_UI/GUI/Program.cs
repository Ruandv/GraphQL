using Innovent_BL;
using Innovent_BL.EmailClient;
using Innovent_BL.SmsClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            const string loggerTemplate = @"{Message:lj}{NewLine}{Exception}";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var logfile = Path.Combine(baseDir, "App_Data", "logs", "Innovent_SimControl.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.With(new ThreadIdEnricher())
                .Enrich.FromLogContext()
                .WriteTo.Console(LogEventLevel.Information, loggerTemplate, theme: AnsiConsoleTheme.Literate)
                .WriteTo.File(logfile, LogEventLevel.Information, loggerTemplate,
                    rollingInterval: RollingInterval.Day, retainedFileCountLimit: 90)
                .CreateLogger();

            var host = Host.CreateDefaultBuilder()
                 .ConfigureAppConfiguration((context, builder) =>
                 {
                     builder.AddJsonFile(@"configuration\appsettings.json", optional: true);
                 })
                 .ConfigureServices((context, services) =>
                 {
                     ConfigureServices(context.Configuration, services);
                 })
                 .ConfigureLogging(logging =>
                 {
                     logging.ClearProviders();
                     logging.AddSerilog();
                 })
                 .Build();

            var services = host.Services;
            var mainForm = services.GetRequiredService<Form1>();
            Application.Run(mainForm);
        }
        private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.Configure<EmailConfigOptions>(configuration.GetSection(EmailConfigOptions.SectionDescription));
            services.Configure<InnoventSettingOptions>(configuration.GetSection(InnoventSettingOptions.SectionDescription));
            services.Configure<SmsConfigOptions>(configuration.GetSection(SmsConfigOptions.SectionDescription));
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<ISmsClient, SmsClient>();
            services.AddSingleton<Form1>();
        }
    }
}
