using System.Text;
using TeamSpeak3QueryApi.Net.Specialized;
using TeamSpeakBOT.Helper;
using TeamSpeakBOT.Interface;

namespace TeamSpeakBOT.Admins;
public class UpdateOnlineAdmins : IModule
{
    private Dictionary<string, int> _serverGroups;
    private List<string> _admins;
    private string _adminNamesInLine = String.Empty;
    private List<int> _adminIdListCache = null;
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
            await FindChannelIdToChange();
            await LoadServerGroups();
            await LoadAdminGroups();
            _isSetted = true;
        }
    }

    public async Task<bool> Run()
    {
        await SetVariables();
        Logger.WriteConsoleAsync("Aktif admin sayısı kontrol ediliyor...");
        int adminCount = await GetAdminCount();
        Logger.WriteConsoleAsync($"Aktif admin sayısı {adminCount} olarak bulundu.");


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
    private async Task FindChannelIdToChange()
    {
        Logger.WriteConsoleAsync("Aktif yetkili kanalının ID'si alınıyor.");
        var channels = await Ts3Client.Client.GetChannels();
        _channelID = channels.Where(x => x.Name.Contains("Aktif Yetkili")).First().Id;
        Logger.WriteConsoleAsync($"Aktif yetkili kanalının ID'si {_channelID} olarak bulundu.");
    }

    private async Task<List<int>> GetAdminIds()
    {
        if (_adminIdListCache is null)
        {
            _adminIdListCache = new List<int>();

            foreach (string name in _admins)
            {
                _adminIdListCache.Add(_serverGroups[name]);
            }
            return _adminIdListCache;
        }
        else return _adminIdListCache;
    }

    private async Task LoadServerGroups()
    {
        Logger.WriteConsoleAsync($"Sunucu rolleri yükleniyor...", LogLevel.Warning);

        var tempGroupsList = await Ts3Client.Client.GetServerGroups();
        StringBuilder sb = new StringBuilder();
        foreach (var group in tempGroupsList)
        {
            _serverGroups.Add(group.Name.Trim(), group.Id);
            sb.Append(group.Name.Trim());
            sb.Append(", ");
        }
        Logger.WriteConsoleAsync(sb.ToString());
        Logger.WriteConsoleAsync($"Toplam {tempGroupsList.Count} rol bulundu.");
    }
    private async Task LoadAdminGroups()
    {

        // Bu roller her TS sunucuna özeldir! Yeni sunucularda isimleri doğru bir şekilde girilmeli!
        await Logger.WriteConsoleAsync($"Admin olarak kabul edilen roller ayarlanıyor...", LogLevel.Warning);
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
        StringBuilder sb = new StringBuilder();

        _admins.ForEach(async x =>
        {
            sb.Append(x.Trim());
            sb.Append(", ");
        });
        await Logger.WriteConsoleAsync(sb.ToString());
    }

    private static string GetOnlineStatusText(string onlineCount)
    {
        return string.Format($"[cspacer]Aktif Yetkili : [{onlineCount}]");
    }
}
