using System.Text.Json.Serialization;

namespace Ts3Bot.Models;

public class SteamServerResponse
{
    [JsonPropertyName("response")]
    public SteamResponse Response { get; set; } = new();
}

public class SteamResponse
{
    [JsonPropertyName("servers")]
    public List<SteamServerInfo> Servers { get; set; } = new();
}

public class SteamServerInfo
{
    [JsonPropertyName("addr")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("gameport")]
    public int GamePort { get; set; }

    [JsonPropertyName("steamid")]
    public string SteamId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("gamedir")]
    public string GameDir { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public int Region { get; set; }

    [JsonPropertyName("players")]
    public int Players { get; set; }

    [JsonPropertyName("max_players")]
    public int MaxPlayers { get; set; }

    [JsonPropertyName("bots")]
    public int Bots { get; set; }

    [JsonPropertyName("map")]
    public string Map { get; set; } = string.Empty;

    [JsonPropertyName("secure")]
    public bool Secure { get; set; }

    [JsonPropertyName("dedicated")]
    public bool Dedicated { get; set; }

    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("gametype")]
    public string GameType { get; set; } = string.Empty;
}
