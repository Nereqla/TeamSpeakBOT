namespace TeamSpeakBOT.Helper;
public static class Logger
{
    private readonly static string _path = AppDomain.CurrentDomain.BaseDirectory;
    private readonly static string _logFileName = "logs\\log.txt";

    public static void LogToFile(string msg)
    {
        CheckLogFolder();
        string filePath = Path.Combine(_path, _logFileName);
        string logText = String.Format($"[{IstanbulTime.GetString}] - {msg}\n");
        File.AppendAllText(filePath, logText);
    }

    private static void CheckLogFolder()
    {
        var folderPath = Path.Combine(_path, "logs");
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
    }

    public static async Task WriteConsoleAsync(string msg)
    {
        await Console.Out.WriteLineAsync($"[{IstanbulTime.GetString}] - {msg}");
    }
}
