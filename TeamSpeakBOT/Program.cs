using TeamSpeakBOT.Admins;
using TeamSpeakBOT.Helper;
using TeamSpeakBOT.Interface;

namespace TeamSpeakBOT;

internal class Program
{

    static async Task Main(string[] args)
    {
        Console.Title = "Teamspeak 3 Simple Bot - Nereqla";
        await Ts3Client.ConnectToServer();
        //await Ts3Client.Client.ChangeNickName("Turk Altis Life BOT");
        await Ts3Client.Client.ChangeNickName("BeowulfBOT");
        await Task.Delay(1000);


        await Logger.WriteConsoleAsync("Yeni kullanıcı takibi başlatılıyor.");
        WatchNewUsers watchNewUsers = new WatchNewUsers();
        await watchNewUsers.StartWatch();
        await Logger.WriteConsoleAsync("Yeni kullanıcı takibi başarıyla başladı.");


        await Logger.WriteConsoleAsync("Modüller yükleniyor..",LogLevel.Warning);
        List <IModule> modules = new List<IModule>()
        {
            new UpdateOnlineAdmins(),
            new UpdateOnlineUsers()
        };

        // ldkfjldsf

        var asda = await Ts3Client.Client.GetClients();


        foreach (var client in asda)
        {
            Console.WriteLine(client.NickName);        }

        int errorLimit = 5;
        int errorCount = 0;
        while (true)
        {
            bool allWork = false;
            int workerCount = 0;
            try
            {
                foreach (var module in modules)
                {
                    var check = await module.Run();
                    if (true) workerCount++;
                }
                if (workerCount >= modules.Count)
                {
                    allWork = true;
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                await Logger.WriteConsoleAsync($"HATA! => {ex.Message}",LogLevel.Error);
                await Logger.WriteConsoleAsync($"Hata sayıcı! => {errorCount}", LogLevel.Error);

                Logger.LogToFile($"\n{DateTime.Now} Exception BAŞLANGICI!");
                Logger.LogToFile($"Message! => {ex.Message}");
                Logger.LogToFile($"Source => {ex.Source}");
                Logger.LogToFile($"Data => {ex.Data}");
                Logger.LogToFile($"StackTrace => {ex.StackTrace}");
                Logger.LogToFile($"Hata sayıcı! => {errorCount}");
                Logger.LogToFile($"Exception SON! \n");
            }
            if (allWork) errorCount = 0;
            if (errorCount > errorLimit)
            {
                Logger.LogToFile($"Hata sayacı {errorLimit} limitini aştı, program sonlandırılıyor!");
                await Logger.WriteConsoleAsync($"Hata sayacı arka arkaya 3'ü aştı, program sonlandırılıyor!",LogLevel.Error);
                break;
            }

            await Task.Delay(TimeSpan.FromMinutes(4));
        }
    }
}
