using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ts3Bot.Models;

namespace Ts3Bot.Infrastructure;

public class SteamService(HttpClient httpClient, IOptions<Settings> settings, ILogger<SteamService> logger) : ISteamService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly Settings _settings = settings.Value;
    private readonly ILogger<SteamService> _logger = logger;

    public async Task<SteamServerInfo?> GetServerInfoAsync()
    {
        var apiKey = _settings.SteamCredentials.ApiKey;
        var gameHost = _settings.SteamCredentials.GameHostAddress;

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(gameHost))
        {
            _logger.LogWarning("Steam API anhatarý veya Sunucu adresi konfigürasyon dosyasýnda eksik!");
            return null;
        }

        var url = $"https://api.steampowered.com/IGameServersService/GetServerList/v1/?key={apiKey}&filter=\\addr\\{gameHost}";

        try
        {
            _logger.LogDebug("{Url} adresinden Steam sunucu bilgileri alýnýyor.", url);
            var response = await _httpClient.GetAsync(url);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Steam API isteði baþarýsýz oldu. Durum Kodu: {StatusCode}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var steamResponse = JsonSerializer.Deserialize<SteamServerResponse>(json);

            if (steamResponse?.Response.Servers.Count <= 0)
            {
                _logger.LogWarning("Steam API, {GameHost} adresi için hiçbir oyun sunucusu döndürmedi.", gameHost);
                return null;
            }
            else
            { 
            }

            return steamResponse.Response.Servers.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Steam sunucu bilgileri alýnýrken bir hata oluþtu.");
            return null;
        }
    }
}
