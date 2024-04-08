namespace TeamSpeakBOT.Helper;
public static class IstanbulTime
{
    private static TimeZoneInfo IstanbulTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

    public static DateTime GetFull
    {
        get => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstanbulTimeZone);
    }
    public static string GetString
    {
        get => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstanbulTimeZone).ToString("HH:mm:ss");
    }
}
