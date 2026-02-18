using System.Text.Json.Serialization;

namespace Ts3Bot.Models;

public class Ts3Credentials
{
    [JsonPropertyName("hostName")]
    public string HostName { get; set; } = string.Empty;

    [JsonPropertyName("portNumber")]
    public int PortNumber { get; set; } = 10011; // Default TS3 Port

    [JsonPropertyName("clientLoginName")]
    public string ClientLoginName { get; set; } = string.Empty;

    [JsonPropertyName("clientPassword")]
    public string ClientPassword { get; set; } = string.Empty;
}
