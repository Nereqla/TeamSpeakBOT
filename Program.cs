using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Ts3Bot.Application;
using Ts3Bot.Infrastructure;

namespace Ts3Bot;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "Teamspeak 3 Simple Bot - Nereqla";

        Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine("Serilog Internal Error: " + msg));
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        if (!Directory.Exists(logDirectory))
            Directory.CreateDirectory(logDirectory);

        string logPath = Path.Combine(logDirectory, "log-.txt");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Extensions.Http", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();

        try
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var tempConfig = configBuilder.Build();
            var googleDocId = tempConfig["GoogleDocId"];

            Models.Settings? applicationSettings = null;
            if (!string.IsNullOrEmpty(googleDocId))
            {
                applicationSettings = await ConfigLoader.LoadSettingsFromGoogleDocAsync(googleDocId);
            }
            else
            {
                Log.Fatal("appsettings.json dosyasında GoogleDocId eksik ve kullanılabilir herhangi bir local (yerel) ayar bulunmuyor.");
                Console.WriteLine("Lütfen appsettings.json dosyasına bir GoogleDocId değeri ekleyin.");
                Console.WriteLine("Herhangi bir tuşa basarak çıkış yapın...");
                Console.ReadLine();
                return;
            }

            if (applicationSettings == null)
            {
                Log.Fatal("Google Docs’tan ayarlar yüklenemedi.");
                Console.WriteLine("Herhangi bir tuşa basarak çıkış yapın...");
                Console.ReadLine();
                return;
            }

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    if (applicationSettings != null)
                    {
                        services.AddSingleton(applicationSettings);
                        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(applicationSettings));
                    }
                    else
                    {
                        services.Configure<Models.Settings>(hostContext.Configuration.GetSection("Settings"));
                    }

                    services.AddHttpClient<ISteamService, SteamService>();
                    services.AddSingleton<Ts3ConnectionManager>();
                    services.AddHostedService<PlayerCountWorker>();
                    services.AddHostedService<AdminCountWorker>();
                    services.AddHostedService<ConnectionWorker>();
                    services.AddHostedService<NotifyWorker>();
                })
                .UseSerilog()
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Uygulama başlatılamadı.");
            Console.WriteLine("Kritik bir hata oluştu. Ayrıntılar için lütfen loglar (kayıtlar) dizinini kontrol edin.");
            Console.WriteLine("Herhangi bir tuşa basarak çıkış yapın...");
            Console.ReadLine();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
