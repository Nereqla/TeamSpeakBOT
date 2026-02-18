using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSpeak3QueryApi.Net.Specialized;
using Ts3Bot.Infrastructure;
using Ts3Bot.Models;

namespace Ts3Bot.Application;

public class AdminCountWorker(ILogger<AdminCountWorker> logger, Ts3ConnectionManager ts3Client, IOptions<Settings> settings) : BackgroundService
{
    private readonly ILogger<AdminCountWorker> _logger = logger;
    private readonly Ts3ConnectionManager _ts3Manager = ts3Client;
    private readonly Settings _settings = settings.Value;
    private int _targetChannelId = 0;
    private int _previousAdminCount = -1;
    private const string ChannelNameFilter = "Aktif Yetkili";
    private readonly Dictionary<string, int> _serverGroups = [];
    private readonly List<int> _adminGroupIds = [];
    private bool _isInitialized = false;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);
        _logger.LogInformation("Admin sayacý baþlatýldý.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                if (_isInitialized)
                {
                    await RunUpdateLoop();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AdminCountWorker] döngüsünde hata.");
                if (!_isInitialized)
                {
                    _logger.LogWarning("[AdminCountWorker] Yüklenemedi (Initialize) Bir sonraki döngüde tekrar denenecek.");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }

    private async Task InitializeAsync()
    {
        if (_targetChannelId == 0)
        {
            var channels = await _ts3Manager.GetChannels();
            if (channels == null) { _logger.LogWarning("[AdminCountWorker - channels null]"); return; }

            var targetChannel = channels.FirstOrDefault(x => x.Name.Contains(ChannelNameFilter));
            if (targetChannel != null)
            {
                _targetChannelId = targetChannel.Id;
                _logger.LogInformation("Hedef kanal bulundu: {Name} (ID: {Id})", targetChannel.Name, targetChannel.Id);
            }
            else
            {
                _logger.LogWarning("Hedef kanal '{Filter}' bulunamadý!", ChannelNameFilter);
                return;
            }
        }

        var groups = await _ts3Manager.GetServerGroups();
        if (groups == null) { _logger.LogWarning("[AdminCountWorker - groups null]"); return; }

        _serverGroups.Clear();
        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.Name))
            {
                if (!_serverGroups.ContainsKey(group.Name.Trim()))
                {
                    _serverGroups.Add(group.Name.Trim(), group.Id);
                }
            }
        }

        _adminGroupIds.Clear();
        foreach (var adminGroupName in _settings.CountableAdminGroups)
        {
            if (_serverGroups.TryGetValue(adminGroupName, out int id))
            {
                _adminGroupIds.Add(id);
            }
            else
            {
                _logger.LogWarning("'{Name}' isimli admin yetkisi bulunamadý.", adminGroupName);
            }
        }

        _isInitialized = true;
    }

    private async Task RunUpdateLoop()
    {
        var onlineClients = await _ts3Manager.GetClients();
        if (onlineClients == null) { _logger.LogWarning("[RunUpdateLoop - onlineClients null]"); return; }

        int currentAdminCount = 0;
        var adminNames = new List<string>();

        foreach (var client in onlineClients)
        {
            try
            {
                var detailedInfo = await _ts3Manager.GetClientInfo(client.Id);
                if (onlineClients == null) { _logger.LogWarning("[RunUpdateLoop - onlineClients null]"); return; }


                if (detailedInfo.ServerGroupIds.Any(gId => _adminGroupIds.Contains(gId)))
                {
                    currentAdminCount++;
                    adminNames.Add(detailedInfo.NickName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Client bilgileri çekilemedi! User ID: {Id} | UserName: {NickName}\nException MSG: {Message}", client.Id, client.NickName, ex.Message);
            }
        }

        _logger.LogInformation("Aktif admin: {Count}. Adminler: {Names}", currentAdminCount, string.Join(", ", adminNames));

        if (currentAdminCount != _previousAdminCount)
        {
            string newName = GetOnlineStatusText(currentAdminCount.ToString());
            await _ts3Manager.EditChannel(_targetChannelId, ChannelEdit.channel_name, newName);

            _logger.LogInformation("Kanal ismi güncellendi: {Name}", newName);
            _previousAdminCount = currentAdminCount;
        }
    }

    private static string GetOnlineStatusText(string onlineCount)
    {
        return string.Format($"[cspacer]Aktif Yetkili : [{onlineCount}]");
    }
}
