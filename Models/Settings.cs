using System.Text.Json.Serialization;

namespace Ts3Bot.Models;

public class Settings
{
    [JsonPropertyName("ts3Credentials")]
    public Ts3Credentials Ts3Credentials { get; set; } = new();

    [JsonPropertyName("steamCredentials")]
    public SteamCredentials SteamCredentials { get; set; } = new();

    [JsonPropertyName("countableAdminGroups")]
    public List<string> CountableAdminGroups { get; set; } = new();

    [JsonPropertyName("notifiableAdminGroups")]
    public List<string> NotifiableAdminGroups { get; set; } = [];

    [JsonPropertyName("welcomeChannelName")]
    public string WelcomeChannelName { get; set; } = "Hoşgeldiniz";

    [JsonPropertyName("newUserGroupName")]
    public string NewUserGroupName { get; set; } = "Yeni Üye";


}
