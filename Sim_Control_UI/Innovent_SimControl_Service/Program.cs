using System;
using System.IO;
using Innovent_BL;
using Innovent_BL.EmailClient;
using Innovent_BL.SmsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Innovent_SimControl_Service
{
    public class Program
    {
        public static void Main(string[] args)
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
            try
            {
                Log.Information("====================================================================");
                Log.Information($"Application Starts. Version: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}");
                //Log.Information($"Application Directory: {AppDomain.CurrentDomain.BaseDirectory}");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Application terminated unexpectedly");
            }
            finally
            {
                Log.Information("====================================================================\r\n");
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Configure the app here.
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<EmailConfigOptions>(hostContext.Configuration.GetSection(EmailConfigOptions.SectionDescription));
                    services.Configure<InnoventSettingOptions>(hostContext.Configuration.GetSection(InnoventSettingOptions.SectionDescription));
                    services.Configure<SmsConfigOptions>(hostContext.Configuration.GetSection(SmsConfigOptions.SectionDescription));
                    services.AddSingleton<IEmailSender, EmailSender>();
                    services.AddSingleton<ISmsClient, SmsClient>();
                    services.AddHostedService<Worker>();
                })
                .UseSerilog();

    }
}
