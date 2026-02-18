using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ts3Bot.Infrastructure;

namespace Ts3Bot.Application;

public class ConnectionWorker : BackgroundService
{
    private readonly ILogger<ConnectionWorker> _logger;
    private readonly Ts3ConnectionManager _ts3Manager;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public ConnectionWorker(ILogger<ConnectionWorker> logger, Ts3ConnectionManager ts3Client, IHostApplicationLifetime hostApplicationLifetime)
    {
        _logger = logger;
        _ts3Manager = ts3Client;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _ts3Manager.ConnectToServer();
            _logger.LogInformation("Nereqla Türk Altis Life Teamspeak botu başarıyla bağlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "ConnectionWorker'da bağlanırken kritik bir bağlantı hatası meydana geldi.");
            _logger.LogWarning("Teamspeak server'ına bağlanılamadı! Daha fazla bilgi için log dosyasına bakınız.");

            _hostApplicationLifetime.StopApplication();
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogInformation("[Zamanlayıcı] 30 dakikalık periyot doldu. Zorunlu bakım başlatılıyor...");

                await _ts3Manager.ForceMaintenanceAsync();

                _logger.LogInformation("[Zamanlayıcı] Bakım başarıyla tamamlandı.");
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Zamanlayıcı] Bakım sırasında hata oluştu! Bir sonraki döngüde tekrar denenecek.");
            }
        }

        _logger.LogInformation("Bot durduruluyor...");
    }
}
