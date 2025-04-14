using TeamSpeak3QueryApi.Net.Specialized.Notifications;
using TeamSpeak3QueryApi.Net.Specialized.Responses;
using TeamSpeakBOT.Helper;

namespace TeamSpeakBOT.Admins;
internal class WatchNewUsers
{
    private bool _enabled = false;
    private int _userBlackListCoolDown = 15;

    private Dictionary<string, DateTime> _userBlackList = new Dictionary<string, DateTime>();

    private static Dictionary<string, int> _serveGroupsNameAndId = new Dictionary<string, int>();
    private int _welcomeChannelID = -1;
    private string _unregisteredUserRoleID = null;
    private string _welcomeChannelName = "[cspacer]•● H σ ş g є ℓ ∂ i η i z ●•";
    private string _unregisteredUserRoleName = "Lütfen Kayıt Olunuz!!.";

    // UYARI!!
    /* 
        BU DOSYADA NEREDEYSE HER ŞEY SABİT DEĞERLERDEN OLUŞMAKTADIR.
        ÖZEL KULLANIMLAR İÇİN METİNLERİN DÜZENLENMESİ GEREKEBİLİR!
     */

    public async Task<bool> StartWatch()
    {
        if (!_enabled)
        {
            await SetVariables();
            await SubscribeChannel();
            return await Task.FromResult(true);
        }
        return await Task.FromResult(true);
    }

    private async Task SubscribeChannel()
    {
        if (_welcomeChannelID < 0)
        {
            var channels = await Ts3Client.Client.GetChannels();

            if (channels.Count <= 0)
                throw new Exception("Hiçbir channel bulunamadı!");

            var welcomeChannel = channels.Where(x => x.Name == _welcomeChannelName).SingleOrDefault();

            if (welcomeChannel is not null)
            {
                _welcomeChannelID = welcomeChannel.Id;
            }
            else
            {
                throw new Exception("Hoşgeldiniz kanalı bulunamadı veya birden fazla eşleşme var!");
            }
        }

        await Ts3Client.Client.RegisterChannelNotification(_welcomeChannelID);
        Logger.WriteConsoleAsync($"Hoşgeldiniz kanalına bağlanıldı. ID:{_welcomeChannelID}", LogLevel.Warning);

        Ts3Client.Client.Subscribe<ClientEnterView>(async data =>
        {
            Logger.WriteConsoleAsync($"Hoş geldiniz kanalında hareketlilik.");
            foreach (var client in data)
            {
                if (client.ServerGroups.Split(',').Any(x => x == _unregisteredUserRoleID.ToString()))
                {
                    Logger.WriteConsoleAsync($"{client.NickName} isimli kayıtsız bir kullanıcı bulundu. Mesaj gönderiliyor..");
                    _ = Ts3Client.Client.SendMessage($"[b][color=red] Türk Altis Life Teamspeak3 sunucusuna hoş geldin, [color=green] {client.NickName}. [color=black]Kayıt bekleme odasına giriş yaptığın takdirde yetkililerimiz sizinle ilgilenecektir.", new GetClientInfo()
                    {
                        Id = client.Id,
                        NickName = client.NickName,
                        DatabaseId = client.DatabaseId,
                        Type = client.Type
                    });
                    await Task.Delay(250);
                    Logger.WriteConsoleAsync($"İlgili yetkililere mesaj gönderiliyor..");
                    bool isAdminsNotified = await NotifyAllAdminsForNewUser(client.Uid, client.NickName);

                    if (isAdminsNotified)
                    {
                        await Logger.WriteConsoleAsync($"Kayıtsız bir kullanıcı sunucuya giriş yaptı: {client.NickName} - İlgili yetkililere mesaj gönderildi.");
                    }
                    else await Logger.WriteConsoleAsync($"Kayıtsız bir kullanıcı sunucuya giriş yaptı: {client.NickName} - Mesaj gönderilmedi, daha önce gönderilmiş. {_userBlackListCoolDown}DK beklemede.", LogLevel.Warning);
                }
            }
            //Logger.WriteConsoleAsync($"CallBack Function bitti.");
        });
        Logger.WriteConsoleAsync($"Hoşgeldiniz kanalı dinleniyor.. ID:{_welcomeChannelID}",LogLevel.Warning);
    }

    private async Task<bool> NotifyAllAdminsForNewUser(string uId, string nickName)
    {
        List<string> serverGroups = new List<string>()
        {
            "Oyun Yöneticisi+",
            "Oyun Yöneticisi",
            "Admin",
            "Baş Rehber",
            "Uzman Rehber",
            "Rehber",
            "Stajyer Rehber"
        };

        List<GetClientInfo> onlineAdmins = new List<GetClientInfo>();

        //blacklist
        if (CheckUId(uId))
        {
            var onlineClients = await Ts3Client.Client.GetClients();
            Logger.WriteConsoleAsync($"TeamSpeak'deki aktif client'lar çekildi. Toplam {onlineClients.Count} kullanıcı aktif.");

            foreach (var client in onlineClients)
            {
                var detailedClientInfo = await Ts3Client.Client.GetClientInfo(client);
                if (detailedClientInfo == null)
                {
                    await Logger.WriteConsoleAsync($"HATA! - {client.NickName} isimli client'ın detaylı bilgileri çekilemedi atlanıyor! (Yetkilerine bakamadık, Potansiyel Yetkili!)",LogLevel.Error);
                    continue;
                }

                try
                {
                    if (detailedClientInfo.ServerGroupIds.Any(x => serverGroups.Any(y => _serveGroupsNameAndId[y] == x)))
                    {
                        onlineAdmins.Add(new GetClientInfo()
                        {
                            NickName = detailedClientInfo.NickName,
                            Id = client.Id,
                            ChannelId = client.ChannelId,
                            DatabaseId = client.DatabaseId,
                            Type = client.Type
                        });
                    }
                }
                catch(Exception ex)
                {
                    Logger.LogToFile($"****************************************************\nMessage:{ex.Message} - \nStackTrace:{ex.StackTrace} - \nData:{ex.Data} - \nSource:{ex.Source}");
                }
            }

            string adminsInline = "";
            foreach (var client in onlineAdmins)
            {
                await Ts3Client.Client.SendMessage($"[b]Kayıtsız bir kullanıcı sunucuya giriş yaptı! - [color=green]{nickName}", client);
                adminsInline += $"[{client.NickName}] ";
            }

            if (adminsInline == "")
                return false;

            adminsInline = adminsInline.Trim().Replace("] [", "] - [");
            await Logger.WriteConsoleAsync($"Mesaj gönderilen yetkililer: " + adminsInline);
            return true;
        }
        return false;
    }

    private bool CheckUId(string uId)
    {
        bool isUserInBlackList = _userBlackList.Keys.Any(x => x.Equals(uId));

        if (isUserInBlackList)
        {
            if (CheckTimeDifference(_userBlackList[uId], _userBlackListCoolDown))
            {
                _userBlackList[uId] = DateTime.Now;
                return true;
            }
            else return false;
        }
        _userBlackList.Add(uId, DateTime.Now);
        return true;
    }
    private static bool CheckTimeDifference(DateTime pastTime, int minutes)
    {
        TimeSpan difference = DateTime.Now - pastTime;
        return difference.TotalMinutes >= minutes;
    }

    private async Task SetVariables()
    {
        var tempGroupsList = await Ts3Client.Client.GetServerGroups();

        foreach (var group in tempGroupsList)
        {
            _serveGroupsNameAndId.Add(group.Name.Trim(), group.Id);

            if (group.Name == _unregisteredUserRoleName)
            {
                _unregisteredUserRoleID = group.Id.ToString();
            }
        }

        if (_unregisteredUserRoleID is null)
            throw new Exception("Kayıtsız kullanıcı rol ID'si bulunamadı!");
    }
}
