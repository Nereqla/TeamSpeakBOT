using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeamSpeak3QueryApi.Net.Specialized;
using Ts3Bot.Infrastructure;

namespace Ts3Bot.Application;

public class PlayerCountWorker(ILogger<PlayerCountWorker> logger, Ts3ConnectionManager ts3Client, ISteamService steamService) : BackgroundService
{
    private readonly ILogger<PlayerCountWorker> _logger = logger;
    private readonly Ts3ConnectionManager _ts3Manager = ts3Client;
    private readonly ISteamService _steamService = steamService;
    private int _targetChannelId = 0;
    private string _previousCount = "0";
    private const string _channelNameFilter = "Aktif Oyuncu";
    private readonly TimeSpan _loopInterval = TimeSpan.FromSeconds(90);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        _logger.LogInformation("Oyuncu sayacý modülü baþlatýldý.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunUpdateLoop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PlayerCountWorker] döngüsünde bir hata gerçekleþti.");
            }

            await Task.Delay(_loopInterval, stoppingToken);
        }
    }

    private async Task RunUpdateLoop()
    {
        if (_targetChannelId == 0)
        {
            var channels = await _ts3Manager.GetChannels();
            if (channels == null) { _logger.LogWarning("[RunUpdateLoop - channels null]"); return; }

            var targetChannel = channels.FirstOrDefault(x => x.Name.Contains(_channelNameFilter));

            if (targetChannel == null)
            {
                _logger.LogWarning("Hedef kanal '{Filter}' bulunamadý!", _channelNameFilter);
                return;
            }
            _targetChannelId = targetChannel.Id;
            _logger.LogInformation("Hedef kanal bulundu: {Name} (ID: {Id})", targetChannel.Name, targetChannel.Id);
        }

        var serverInfo = await _steamService.GetServerInfoAsync();
        string playerCount;
        if (serverInfo != null) playerCount = serverInfo.Players.ToString();
        else
        {
            _logger.LogWarning("serverInfo null geldi, player count 0 olarak ayarlandý.");
            playerCount = "0";
        }

        if (playerCount != _previousCount)
        {
            _logger.LogInformation("Oyuncu sayýsý {Old} kiþiden {New} kiþiye deðiþtirildi.", _previousCount, playerCount);

            var newChannelName = $"[cspacer]Aktif Oyuncu : [{playerCount}]";
            await _ts3Manager.EditChannel(_targetChannelId, ChannelEdit.channel_name, newChannelName);

            _previousCount = playerCount;
            _logger.LogInformation("Kanal baþarýyla güncellendi.");
        }
        else _logger.LogInformation("Oyuncu sayýsý sabit kaldý.");
    }
}
