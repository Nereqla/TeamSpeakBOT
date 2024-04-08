using TeamSpeak3QueryApi.Net.Specialized;

namespace TeamSpeakBOT.Helper;
public static class Ts3Client
{
    private static TeamSpeakClient _client;
    public static TeamSpeakClient? Client
    {
        get
        {
            lock (new object())
            {
                return _client;
            }
        }
    }

    public static async Task ConnectToServer()
    {
        _client = new TeamSpeakClient(Ts3Credentials.HostName, Ts3Credentials.PortNumber);
        await Logger.WriteConsoleAsync("Bağlanılıyor..");
        await _client.Connect();
        await Logger.WriteConsoleAsync($"Bağlanıldı!");



        await _client.Login(Ts3Credentials.ClientLoginName, Ts3Credentials.ClientPassword);



        await Logger.WriteConsoleAsync($"{Ts3Credentials.ClientLoginName} adlı hesaba bağlanıldı");
        await _client.UseServer(1);
    }
}
