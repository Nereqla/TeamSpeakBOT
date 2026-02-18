using System.Text.Json.Serialization;

namespace Ts3Bot.Models;

public class SteamCredentials
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("gameHostAddress")]
    public string GameHostAddress { get; set; } = string.Empty;
}
