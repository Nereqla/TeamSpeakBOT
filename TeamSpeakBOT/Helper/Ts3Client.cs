using TeamSpeak3QueryApi.Net.Specialized;

namespace TeamSpeakBOT.Helper;
public static class Ts3Client
{
    private static TeamSpeakClient _client;

    private static readonly Lock _lock = new Lock();
    public static TeamSpeakClient? Client
    {
        get
        {
            lock (_lock)
            {
                return _client;
            }
        }
    }

    public static async Task ConnectToServer()
    {
        _client = new TeamSpeakClient(Ts3Credentials.HostName, Ts3Credentials.PortNumber);
        await Logger.WriteConsoleAsync("Bağlanılıyor..",LogLevel.Warning);
        await _client.Connect();
        await Logger.WriteConsoleAsync($"Bağlanıldı!", LogLevel.Warning);

        await _client.Login(Ts3Credentials.ClientLoginName, Ts3Credentials.ClientPassword);

        var servers = await _client.GetServers();

        if (servers.Count < 1) throw new Exception("Herhangi bir sunucu bulunamadı!");

        var serverID = servers.FirstOrDefault().Id;

        await Logger.WriteConsoleAsync($"{Ts3Credentials.ClientLoginName} adlı hesaba bağlanıldı", LogLevel.Warning);
        await _client.UseServer(serverID);
    }
}
