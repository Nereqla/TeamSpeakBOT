using Spectre.Console;

namespace TeamSpeakBOT.Helper;
public static class Logger
{
    private readonly static string _path = AppDomain.CurrentDomain.BaseDirectory;
    private readonly static string _logFileName = "logs\\log.txt";
    private static readonly Lock _lock = new Lock();

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

    public static async Task WriteConsoleAsync(string msg, LogLevel level = LogLevel.Info)
    {
        using (_lock.EnterScope())
        {
            string timestamp = IstanbulTime.GetString;
            msg = msg.Replace("[", "[[").Replace("]", "]]");

            string levelText;
            Color levelColor;
            switch (level)
            {
                case LogLevel.Info:
                    levelText = "INFO";
                    levelColor = Color.Green;
                    break;
                case LogLevel.Warning:
                    levelText = "WARN";
                    levelColor = Color.Yellow;
                    break;
                case LogLevel.Error:
                    levelText = "ERROR";
                    levelColor = Color.Red;
                    break;
                default:
                    levelText = "INFO";
                    levelColor = Color.Green;
                    break;
            }

            AnsiConsole.MarkupLine(
                $"[cyan]{timestamp}[/] " +
                $"[white][{levelColor}]{levelText}[/][/] " +
                $"[white]- {msg}[/]"
            );
        }
    }
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}
