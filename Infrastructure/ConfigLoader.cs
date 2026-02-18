using Serilog;
using System.Text.Json;
using Ts3Bot.Helpers;
using Ts3Bot.Models;

namespace Ts3Bot.Infrastructure;

public static class ConfigLoader
{
    public static async Task<Settings?> LoadSettingsFromGoogleDocAsync(string docId)
    {
        if (string.IsNullOrEmpty(docId))
        {
            return null;
        }

        Log.Information("Google Docs’tan ayarlar alýnýyor (ID: {Id})...", docId);

        try
        {
            using var httpClient = new HttpClient();
            var url = GoogleApi.GetSheetsFileRawText(docId);
            var jsonContent = await httpClient.GetStringAsync(url);

            var settings = JsonSerializer.Deserialize<Settings>(jsonContent);

            Log.Information("Ayarlar Google Docs’tan baþarýyla yüklendi.");
            return settings;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Google Docs’tan ayarlar yüklenemedi! Uygulama sonlandýrýlýyor.");

            Console.WriteLine("Herhangi bir tuþa basarak çýkýþ yapýn...");
            Console.ReadLine();
            Environment.Exit(0);
            return null;
        }
    }
}
