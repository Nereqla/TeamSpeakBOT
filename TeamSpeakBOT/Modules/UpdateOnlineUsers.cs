using System.Data;
using System.Net;
using System.Text.Json;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeakBOT.Helper;
using TeamSpeakBOT.Interface;
using TeamSpeakBOT.Models;

namespace TeamSpeakBOT.Admins;
internal class UpdateOnlineUsers : IModule
{
    private int _channelID = 0;
    private string _apiURL;
    private HttpClient _httpClient;
    private string _previousCount = String.Empty;
    private bool _isSetted = false;
    private string _armaHostName = "Game Server IP Address";

    public UpdateOnlineUsers()
    {
        _httpClient = new HttpClient();
    }
    private async Task SetVariables()
    {
        if (!_isSetted)
        {
            await FindChannelIdToChange();
            _isSetted = true;
            SetApiURL();
        }
    }


    private void SetApiURL()
    {
        _apiURL = String.Format($"https://api.steampowered.com/IGameServersService/GetServerList/v1/?key={SteamCredentials.SteamApiKey}&filter=addr\\{_armaHostName}");
    }
    public async Task<bool> Run()
    {
        await SetVariables();
        Logger.WriteConsoleAsync("Steam API'a istek atılıyor. ", LogLevel.Warning);
        string newestCount = await GetOnlineCount();
        Logger.WriteConsoleAsync($"Sunucunun oyuncu sayısı {newestCount} olarak bulundu.");

        if (newestCount != _previousCount)
        {
            Ts3Client.Client.EditChannel(_channelID, ChannelEdit.channel_name, GetOnlineStatusText(newestCount));
            await Logger.WriteConsoleAsync($"Online oyuncu sayısı {newestCount} olarak güncellendi!");
            _previousCount = newestCount;
        }
        else
        {
            await Logger.WriteConsoleAsync($"Online oyuncu sayısı kontrol edildi! Değişen bir şey yok. Güncel sayı: {newestCount}", LogLevel.Warning);
        }

        return true;
    }

    private async Task<string> GetOnlineCount()
    {
        SteamServerJsonResponse jsonObject = null;
        try
        {
            var response = await _httpClient.GetAsync(_apiURL);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogToFile($"HATA! Status Code: {(int)response.StatusCode}:{response.StatusCode}");
            }
            string json = await response.Content.ReadAsStringAsync();
            jsonObject = JsonSerializer.Deserialize<SteamServerJsonResponse>(json);
        }
        catch (Exception ex)
        {
            Logger.LogToFile("HATA! Steam API'da bir sorun çıktı!");
            Logger.LogToFile("=> GetOnlineCount()");
            Logger.LogToFile($"Error message: {ex.Message}");
            throw new Exception("Steam api hata verdi, modül bu seferlik atlandı.");
        }

        if (jsonObject == null)
        {
            await Logger.WriteConsoleAsync($"GetOnlineCount() methounda jsonObject null!", LogLevel.Error);
            Logger.LogToFile("GetOnlineCount() methounda jsonObject null!");
            throw new Exception("GetOnlineCount() methounda jsonObject null!");
        }

        if (jsonObject.response.servers.Count <= 0)
        {
            await Logger.WriteConsoleAsync("Arma sunucusu bulunamadı[!] Oyuncu sayısı 0 olarak giriliyor." ,LogLevel.Warning);
            Logger.LogToFile("Arma sunucu bulunamadı? Değer 0 olarak varsayılıyor.");
            return "0";
        }
        else return jsonObject.response.servers.FirstOrDefault().players.ToString();
    }
    private async Task FindChannelIdToChange()
    {
        Logger.WriteConsoleAsync("Aktif yetkili kanalının ID'si alınıyor.", LogLevel.Warning);
        var channels = await Ts3Client.Client.GetChannels();
        _channelID = channels.Where(x => x.Name.Contains("Aktif Oyuncu")).Single().Id;
        Logger.WriteConsoleAsync($"Aktif yetkili kanalının ID'si {_channelID} olarak bulundu.");
    }
    private static string GetOnlineStatusText(string onlineCount)
    {
        return string.Format($"[cspacer]Aktif Oyuncu : [{onlineCount}]");
    }
}
