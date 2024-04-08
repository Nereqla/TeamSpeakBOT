using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeakBOT.Helper;
using TeamSpeakBOT.Interface;

namespace TeamSpeakBOT.Admins;
public class UpdateOnlineAdmins : IModule
{
    private Dictionary<string, int> _serverGroups;

    private List<string> _admins;

    private string _adminNamesInLine = String.Empty;

    private int _previousAdminCount = 0;

    private int _channelID = 0;

    private bool _isSetted = false;


    public UpdateOnlineAdmins()
    {
        _serverGroups = new Dictionary<string, int>();
    }

    private async Task SetVariables()
    {
        if (!_isSetted)
        {
            var channels = await Ts3Client.Client.GetChannels();
            _channelID = channels.Where(x => x.Name.Contains("Aktif Yetkili")).First().Id;
            await LoadServerGroups();
            await LoadAdminGroups();
            _isSetted = true;
        }
    }

    public async Task<bool> Run()
    {
        await SetVariables();
        int adminCount = await GetAdminCount();

        if (adminCount != _previousAdminCount)
        {
            _previousAdminCount = adminCount;
            await Ts3Client.Client.EditChannel(_channelID, ChannelEdit.channel_name, GetOnlineStatusText(adminCount.ToString()));
            await Logger.WriteConsoleAsync($"Online admin sayısı {adminCount} olarak güncellendi!");
            await Logger.WriteConsoleAsync($"Aktif adminler: {_adminNamesInLine}");
        }
        else
        {
            await Logger.WriteConsoleAsync($"Admin sayısı hâlâ aynı gözüküyor! Güncel sayı: {adminCount}");
        }
        return true;
    }

    private async Task<int> GetAdminCount()
    {
        var onlineClients = await Ts3Client.Client.GetClients();
        var adminIdList = await GetAdminIds();
        int counter = 0;
        _adminNamesInLine = String.Empty;
        foreach (var client in onlineClients)
        {
            var user = await Ts3Client.Client.GetClientInfo(client);
            if (user.ServerGroupIds.Any(x => adminIdList.Any(y => y == x)))
            {
                _adminNamesInLine += String.Format($"[{user.NickName}] ");

                counter++;
            }
        }
        return counter;
    }

    private async Task<List<int>> GetAdminIds()
    {
        var tempList = new List<int>();

        foreach (string name in _admins)
        {
            tempList.Add(_serverGroups[name]);
        }

        return tempList;
    }

    private async Task LoadServerGroups()
    {
        var tempGroupsList = await Ts3Client.Client.GetServerGroups();

        foreach (var group in tempGroupsList)
        {
            _serverGroups.Add(group.Name.Trim(), group.Id);
        }
    }
    private async Task LoadAdminGroups()
    {
        _admins = new List<string>
        {
            "Moderatör",
            "Stajyer Rehber",
            "Oyun Yöneticisi",
            "Rehber",
            "Uzman Rehber",
            "Admin",
            "Oyun Yöneticisi+",
            "Founder",
            "Yönetici",
            "Scripter",
            "Yönetim Sorumlusu",
        };
    }

    private static string GetOnlineStatusText(string onlineCount)
    {
        return string.Format($"[cspacer]Aktif Yetkili : [{onlineCount}]");
    }
}
