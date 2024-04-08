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
        Ts3Client.Client.ChangeNickName("Turk Altis Life");
        await Task.Delay(1000);


        WatchNewUsers watchNewUsers = new WatchNewUsers();
        await watchNewUsers.StartWatch();

        List <IModule> modules = new List<IModule>()
        {
            new UpdateOnlineAdmins(),
            new UpdateOnlineUsers()
        };
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
                Logger.LogToFile($"HATA! => {ex.Message}");
                Logger.LogToFile($"Hata sayıcı! => {errorCount}");
                await Logger.WriteConsoleAsync($"HATA! => {ex.Message}");
                await Logger.WriteConsoleAsync($"Hata sayıcı! => {errorCount}");
            }
            if (allWork) errorCount = 0;
            if (errorCount > errorLimit)
            {
                Logger.LogToFile($"Hata sayacı {errorLimit} limitini aştı, program sonlandırılıyor!");
                await Logger.WriteConsoleAsync($"Hata sayacı arka arkaya 3'ü aştı, program sonlandırılıyor!");
                break;
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}
