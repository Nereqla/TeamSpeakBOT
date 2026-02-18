using Ts3Bot.Models;

namespace Ts3Bot.Infrastructure;

public interface ISteamService
{
    Task<SteamServerInfo?> GetServerInfoAsync();
}
