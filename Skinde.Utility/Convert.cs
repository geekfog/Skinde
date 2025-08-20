namespace Skinde.Utility;

public static class Convert
{
    public static string DateTimeCt(DateTime when)
    {
        try
        {
            //var centralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            //var joinedCentralTime = TimeZoneInfo.ConvertTimeFromUtc(when, centralTimeZone);
            //var converted = joinedCentralTime.ToString("yyyy-MM-dd HH:mm:ss");
            //return converted;
            return when.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch
        {
            return string.Empty;
        }
    }
}