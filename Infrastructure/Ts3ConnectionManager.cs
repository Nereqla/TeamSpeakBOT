using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeak3QueryApi.Net.Specialized.Responses;

namespace Ts3Bot.Infrastructure;

public class Ts3ConnectionManager(IOptions<Models.Settings> settings, ILogger<Ts3ConnectionManager> logger)
{
    private readonly ILogger<Ts3ConnectionManager> _logger = logger;
    private readonly Models.Settings _settings = settings.Value;
    private TeamSpeakClient? _client;
    private readonly SemaphoreSlim _clientLock = new(1, 1);
    public Func<Task> OnConnectionRefreshed { get; set; }
    private async Task ConnectInternalAsync()
    {
        var creds = _settings.Ts3Credentials;
        _logger.LogInformation("{Host}:{Port} adresindeki TeamSpeak sunucusuna bağlanılıyor...", creds.HostName, creds.PortNumber);

        _client = new TeamSpeakClient(creds.HostName, creds.PortNumber);
        await _client.Connect();

        _logger.LogInformation("Bağlanıldı! {Login} olarak giriş yapılıyor...", creds.ClientLoginName);
        await _client.Login(creds.ClientLoginName, creds.ClientPassword);

        var servers = await _client.GetServers();
        if (servers.Count == 0) throw new Exception("Hiçbir sunucu bulunamadı!");

        var serverId = servers[0].Id;
        _logger.LogInformation("ID’si {ServerId} olan sunucu seçiliyor...", serverId);
        await _client.UseServer(serverId);

        _logger.LogInformation($"NickName güncelleniyor.");
        await _client.ChangeNickName("Türk Altislife");

        _logger.LogInformation("Bağlantı başarıyla kuruldu ve hazır.");
    }

    public async Task ConnectToServer()
    {
        await _clientLock.WaitAsync();
        try
        {
            await ConnectInternalAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TeamSpeak sunucusuna bağlanılamadı.");
            throw;
        }
        finally
        {
            _clientLock.Release();
        }
    }
    private async Task Run(Func<TeamSpeakClient, Task> action, [CallerMemberName] string methodName = "")
    {
        var traceId = Guid.NewGuid().ToString()[..8];

        _logger.LogTrace("[{TraceId}] '{Method}' (Run) çağrıldı, kilit (semaphore) bekleniyor...", traceId, methodName);

        if (!await _clientLock.WaitAsync(TimeSpan.FromSeconds(40)))
        {
            _logger.LogWarning("[{TraceId}] '{Method}' (Run) Kilit 40 saniyedir alınamadı! Sistem tıkalı.", traceId, methodName);
            return;
        }

        _logger.LogTrace("[{TraceId}] '{Method}' (Run) kilidi aldı, işlem yürütülüyor...", traceId, methodName);

        try
        {
            if (_client == null) throw new InvalidOperationException("İstemci başlatılmadı. (Client NULL)");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));

            await action(_client).WaitAsync(cts.Token);
            _logger.LogTrace("[{TraceId}] '{Method}' (Run) işlem başarıyla tamamlandı.", traceId, methodName);
        }
        catch (TimeoutException)
        {
            _logger.LogError("[{TraceId}] '{Method}' (Query) ZAMAN AŞIMINA UĞRADI! Bağlantı ölmüş olabilir.", traceId, methodName);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] '{Method}' (Run) içinde hata oluştu!", traceId, methodName);
            throw;
        }
        finally
        {
            _clientLock.Release();
            _logger.LogTrace("[{TraceId}] '{Method}' (Run) kilidi bıraktı.", traceId, methodName);
        }
    }

    private async Task<T?> Query<T>(Func<TeamSpeakClient, Task<T>> action, [CallerMemberName] string methodName = "")
    {
        var traceId = Guid.NewGuid().ToString()[..8];

        _logger.LogTrace("[{TraceId}] '{Method}' (Query) çağrıldı, kilit bekleniyor...", traceId, methodName);
        if (!await _clientLock.WaitAsync(TimeSpan.FromSeconds(25)))
        {
            _logger.LogWarning("[{TraceId}] '{Method}' (Query) Kilit 25 saniyedir alınamadı! Sistem tıkalı.", traceId, methodName);
            return default;
        }
        _logger.LogTrace("[{TraceId}] '{Method}' (Query) kilidi aldı, sorgu gönderiliyor...", traceId, methodName);

        try
        {
            if (_client == null) throw new InvalidOperationException("İstemci başlatılmadı.");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            var result = await action(_client).WaitAsync(cts.Token);

            _logger.LogTrace("[{TraceId}] '{Method}' (Query) sonucu döndü.", traceId, methodName);
            return result;
        }
        catch (TimeoutException)
        {
            _logger.LogError("[{TraceId}] '{Method}' (Query) ZAMAN AŞIMINA UĞRADI! Bağlantı ölmüş olabilir.", traceId, methodName);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{TraceId}] '{Method}' (Query) hata fırlattı!", traceId, methodName);
            throw;
        }
        finally
        {
            _clientLock.Release();
            _logger.LogTrace("[{TraceId}] '{Method}' (Query) kilidi bıraktı.", traceId, methodName);
        }
    }

    public async Task ForceMaintenanceAsync()
    {
        if (!await _clientLock.WaitAsync(TimeSpan.FromSeconds(30)))
        {
            _logger.LogCritical("[ForceMaintenanceAsync] Bakım için kilit alınamadı! (Hala çalışan işlem var veya deadlock).");
            return;
        }
        bool connectionSuccess = false;
        try
        {
            _client?.Dispose();
            _client = null;

            await ConnectInternalAsync();

            connectionSuccess= true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ForceMaintenanceAsync] Yenilenme sırasında hata!");
        }
        finally
        {
            _clientLock.Release();
        }

        if (connectionSuccess)
        {
            await OnConnectionRefreshed.Invoke();
        }
    }

    // Proxy methods
    public async Task ChangeNickName(string name) => await Run(c => c.ChangeNickName(name));

    public async Task<GetClientInfo?> GetClientInfoFromNameAsync(string name)
    {
        var clients = await GetClients();
        return clients.FirstOrDefault(c => c.NickName.Equals(name));
    }

    public async Task PokeClient(int clientId, string message) => await Run(c => c.PokeClient(clientId, message));

    public async Task<IReadOnlyList<GetChannelListInfo>?> GetChannels() => await Query(c => c.GetChannels());

    public async Task<IReadOnlyList<GetServerGroupListInfo>?> GetServerGroups() => await Query(c => c.GetServerGroups());

    public async Task<IReadOnlyList<GetClientInfo>?> GetClients() => await Query(c => c.GetClients());

    public async Task<GetClientDetailedInfo?> GetClientInfo(int clientId) => await Query(c => c.GetClientInfo(clientId));

    public async Task SendMessage(string message, GetClientInfo targetClient) => await Run(c => c.SendMessage(message, targetClient));

    public async Task<IReadOnlyCollection<EditChannelInfo>?> EditChannel(int channelid, ChannelEdit editChannel, string value) =>
        await Query(c => c.EditChannel(channelid, editChannel, value));

    public async Task RegisterChannelNotification(int channelId) => await Run(c => c.RegisterChannelNotification(channelId));

    public async Task RegisterServerNotification() => await Run(c => c.RegisterServerNotification());

    public async Task Subscribe<T>(Action<IReadOnlyCollection<T>> callback) where T : TeamSpeak3QueryApi.Net.Specialized.Notifications.Notification
    {
        await Run(client => { client.Subscribe(callback); return Task.CompletedTask; });
    }
}
