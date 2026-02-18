namespace Ts3Bot.Helpers;

public static class GoogleApi
{
    public static string GetFileStaticUri(string fileID)
    {
        return string.Format($"https://drive.google.com/uc?export=download&id={fileID}");
    }
    public static string GetSheetsFileRawText(string sheetID)
    {
        return string.Format($"https://docs.google.com/document/d/{sheetID}/export?format=txt");
    }
}
