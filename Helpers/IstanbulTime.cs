namespace Ts3Bot.Helpers;

public static class IstanbulTime
{
    private static readonly TimeZoneInfo IstanbulTimeZone;

    static IstanbulTime()
    {
        try
        {
            IstanbulTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            IstanbulTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        }
    }

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstanbulTimeZone);

    public static string TimeString => Now.ToString("HH:mm:ss");
}
