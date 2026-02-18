using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using Ts3Bot.Helpers;
using Ts3Bot.Infrastructure;
using Ts3Bot.Models;

namespace Ts3Bot.Application;

public class NotifyWorker(ILogger<NotifyWorker> logger, Ts3ConnectionManager ts3Client, IOptions<Settings> settings) : BackgroundService
{
    private readonly ILogger<NotifyWorker> _logger = logger;
    private readonly Ts3ConnectionManager _ts3Manager = ts3Client;
    private readonly Settings _settings = settings.Value;

    private readonly Dictionary<string, DateTime> _newUserBlackList = [];
    private readonly Dictionary<string, DateTime> _supportJoinBlackList = [];
    private readonly Dictionary<string, int> _serverGroups = [];
    private const int _userBlackListCoolDownMinutes = 15;
    private int _welcomeChannelId;
    private int _newUserGroupId;
    private bool _isInitialized;
    private readonly int _supportChannelID = 60;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);
        await SetupSubscriptions();
    }
    private async Task SetupSubscriptions()
    {
        try
        {
            await LoadServerGroups();
            await FindWelcomeChannelId();
            FindNewUserGroupId();

            await RegisterNotifications();
            //_ts3Client.OnConnectionRefreshed = async () => await RegisterNotifications();
            _ts3Manager.OnConnectionRefreshed = RegisterNotifications;

            _logger.LogInformation("Yeni üye karşılama modülü yüklendi.");

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kanallara abone olma işleminde bir hata oluştu.");
        }
    }
    private async Task RegisterNotifications()
    {
        try
        {
            _logger.LogInformation("Kanallara abone olunuyor.");
            await _ts3Manager.RegisterChannelNotification(_welcomeChannelId);
            await _ts3Manager.RegisterServerNotification();
            _logger.LogInformation("Kanallara abone olundu.");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Kanallara abone olma işleminde bir hata gerçekleşti.");
            return;
        }

        await _ts3Manager.Subscribe<ClientEnterView>(async enters =>
        {
            try
            {
                await OnClientEnterView(enters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscribe eventi işlenirken hata oluştu.");
            }
        });

        await _ts3Manager.Subscribe<ClientMoved>(async moves => {
            try
            {
                await OnClientMoved(moves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClientMoved eventi işlenirken hata oluştu.");
            }
        });

        _logger.LogInformation("Event Handlers yüklendi...");
    }

    private async Task LoadServerGroups()
    {
        var groups = await _ts3Manager.GetServerGroups();
        if (groups == null) { _logger.LogWarning("[LoadServerGroups - groups null]"); return; }

        _serverGroups.Clear();
        foreach (var group in groups)
        {
            if (!string.IsNullOrEmpty(group.Name))
            {
                _serverGroups[group.Name] = group.Id;
            }
        }
    }

    private async Task OnClientEnterView(IReadOnlyCollection<ClientEnterView> views)
    {
        foreach (var client in views)
        {
            await HandleNewUser(client.NickName, client.ServerGroups, client.TargetChannelId, client.Id, client.DatabaseId, client.Type);
        }
    }

    private async Task OnClientMoved(IReadOnlyCollection<ClientMoved> moves)
    {
        foreach (var client in moves)
        {
            if (client.TargetChannel == _supportChannelID)
            {
                await HandleSupportChannelUser(client);
            }
            _logger.LogTrace("[ClientMoved] Invoker: {Invoker} Reason: {Reason} Target: {Target}", client.InvokerName, client.Reason, client.TargetChannel);
        }
    }

    private async Task HandleNewUser(string nickName, string serverGroupIds, int channelId, int clientId, int databaseId, ClientType clientType)
    {
        if (!_isInitialized) return;

        var groupIds = serverGroupIds.Split(',').Select(s => int.TryParse(s, out var i) ? i : -1).ToList();

        if (groupIds.Contains(_newUserGroupId))
        {
            _logger.LogInformation("Kayıtsız bir kullanıcı bulundu: {NickName}", nickName);

            var clientInfo = new GetClientInfo { Id = clientId, NickName = nickName, DatabaseId = databaseId, Type = clientType };
            var msg = $"[b][color=red] Türk Altis Life Teamspeak3 sunucusuna hoş geldin, [color=green] {nickName}. [color=black]Kayıt bekleme odasına giriş yaptığın takdirde yetkililerimiz sizinle ilgilenecektir.";
            await _ts3Manager.SendMessage(msg, clientInfo);

            await NotifyAdmins(clientInfo, NotifyAdminReason.NewUser);
        }
    }

    private async Task HandleSupportChannelUser(ClientMoved clientmoved)
    {
        if (!_isInitialized) return;

        int clientId = clientmoved.ClientIds[0];

        var clientDetailedInfo = await _ts3Manager.GetClientInfo(clientId);
        if (clientDetailedInfo == null) { _logger.LogWarning("[HandleSupportChannelUser - clientDetailedInfo null]"); return; }


        var clientInfo = ConvertDetailedInfoToNormal(clientDetailedInfo, clientId);

        var result = await NotifyAdmins(clientInfo,NotifyAdminReason.Support);

        if (result) await _ts3Manager.PokeClient(clientId,"Yetkililere mesaj gönderildi. Lütfen bekleyiniz.");
    }

    private async Task<bool> NotifyAdmins(GetClientInfo userClient, NotifyAdminReason reason)
    {
        if (reason == NotifyAdminReason.NewUser && !CanNotifyNewUser(userClient.DatabaseId.ToString()))
        {
            _logger.LogInformation($"{userClient.NickName} İsimli kullanıcı için bilgilendirme yapılmış.");
            return false;
        }
        else if (reason == NotifyAdminReason.Support && !CanNotifyForSupport(userClient.DatabaseId.ToString()))
        {
            _logger.LogInformation($"{userClient.NickName} İsimli kullanıcı için bilgilendirme yapılmış.");
            return false;
        }

        string notifyMsg = string.Empty;

        if (NotifyAdminReason.NewUser == reason)
        {
            notifyMsg = $"[b]Kayıtsız bir kullanıcı sunucuya giriş yaptı! - [color=green]{userClient.NickName}";

        }
        else if (NotifyAdminReason.Support == reason)
        { 
            notifyMsg = $"[b]Bir kullanıcı destek odasında! - [color=green]{userClient.NickName}";
        }

        var onlineClients = await _ts3Manager.GetClients();
        if (onlineClients == null) { _logger.LogWarning("[NotifyAdmins - onlineClients null]"); return false; }

        var adminList = new List<GetClientInfo>();

        foreach (var client in onlineClients)
        {
            var detail = await _ts3Manager.GetClientInfo(client.Id);
            if (detail == null) continue;

            foreach (var adminGroupName in _settings.NotifiableAdminGroups)
            {
                if (_serverGroups.TryGetValue(adminGroupName, out var adminGroupId))
                {
                    if (detail.ServerGroupIds.Contains(adminGroupId))
                    {
                        adminList.Add(client);
                        break;
                    }
                }
            }
        }

        if (adminList.Count == 0) return false;

        var adminsInline = string.Join(" - ", adminList.Select(x => $"[{x.NickName}]"));

        foreach (var admin in adminList)
        {
            await _ts3Manager.SendMessage(notifyMsg, admin);
        }

        _logger.LogInformation("Yetkililer {User} isimli kullanıcı için bilgilendirildi: {Admins}", userClient.NickName, adminsInline);
        return true;
    }

    private bool CanNotifyForSupport(string uniqueId)
    {
        if (_supportJoinBlackList.TryGetValue(uniqueId, out var lastSeen))
        {
            if ((DateTime.Now - lastSeen).TotalMinutes < _userBlackListCoolDownMinutes)
            {
                return false;
            }
        }
        _supportJoinBlackList[uniqueId] = DateTime.Now;
        return true;
    }

    private bool CanNotifyNewUser(string uniqueId)
    {
        if (_newUserBlackList.TryGetValue(uniqueId, out var lastSeen))
        {
            if ((DateTime.Now - lastSeen).TotalMinutes < _userBlackListCoolDownMinutes)
            {
                return false;
            }
        }
        _newUserBlackList[uniqueId] = DateTime.Now;
        return true;
    }

    private GetClientInfo ConvertDetailedInfoToNormal(GetClientDetailedInfo detailedInfo, int clientID)
    {
        return new GetClientInfo()
        {
            ChannelId = detailedInfo.ChannelId,
            DatabaseId = detailedInfo.DatabaseId,
            NickName = detailedInfo.NickName,
            Type = detailedInfo.Type,
            Id = clientID
        };
    }

    private void FindNewUserGroupId()
    {
        if (_serverGroups.TryGetValue(_settings.NewUserGroupName, out var id))
        {
            _newUserGroupId = id;
        }
        else
        {
            _logger.LogWarning("Yeni kullanıcı yetkisi '{Name}' sunucu yetkileri içinde bulunamadı!", _settings.NewUserGroupName);
        }
    }

    private async Task FindWelcomeChannelId()
    {
        var channels = await _ts3Manager.GetChannels();
        if (channels == null) { _logger.LogWarning("[FindWelcomeChannelId - channels null]"); return; }


        var channel = channels.FirstOrDefault(c => c.Name.Contains(_settings.WelcomeChannelName));
        if (channel != null)
        {
            _welcomeChannelId = channel.Id;
        }
        else
        {
            _logger.LogWarning("Hoşgeldiniz kanalı için '{Name}' ile herhangi bir eşleşme bulunamadı!", _settings.WelcomeChannelName);
        }
    }
}
