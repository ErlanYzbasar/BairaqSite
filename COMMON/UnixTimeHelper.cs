using System.Globalization;

namespace COMMON;

public static class UnixTimeHelper
{
    public static int GetCurrentUnixTime()
    {
        return ConvertToUnixTime(DateTime.Now);
    }

    public static int ConvertToUnixTime(DateTime datetime)
    {
        return (int)datetime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }

    public static int ConvertToUnixTime(string datetimeStr)
    {
        if (DateTime.TryParse(datetimeStr, out var datetime))
        {
            return ConvertToUnixTime(datetime);
        }

        return 0;
    }

    public static DateTime UnixTimeToDateTime(int unixtime)
    {
        var sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return sTime.AddSeconds(unixtime);
    }

    public static string UnixTimeToStringFromNow(int unixtime)
    {
        var time = UnixTimeToDateTime(unixtime);
        var span = DateTime.Now - time;
        if (span.TotalDays > 365)
        {
            return string.Format("{0} жыл бұрын", (int)(Math.Floor(span.TotalDays) / 365));
        }

        if (span.TotalDays > 30)
        {
            return string.Format("{0} ай бұрын", (int)(Math.Floor(span.TotalDays) / 30));
        }

        if (span.TotalDays > 7)
        {
            return string.Format("{0} апта бұрын", (int)(Math.Floor(span.TotalDays) / 7));
        }

        if (span.TotalDays > 1)
        {
            return string.Format("{0} күн бұрын", (int)Math.Floor(span.TotalDays));
        }

        if (span.TotalHours > 1)
        {
            return
                string.Format("{0} сағат бұрын", (int)Math.Floor(span.TotalHours));
        }

        if (span.TotalMinutes > 1)
        {
            return
                string.Format("{0} минут бұрын", (int)Math.Floor(span.TotalMinutes));
        }

        if (span.TotalSeconds >= 1)
        {
            return
                string.Format("{0} секунд бұрын", (int)Math.Floor(span.TotalSeconds));
        }

        return "1 секунд бұрын";
    }

    public static string GetTime(int unixtime)
    {
        var datetime = UnixTimeToDateTime(unixtime);
        return datetime.ToString("HH:mm");
    }


    public static string AstanaUnixTimeToString(int unixtime)
    {
        var sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var astanaTime = sTime.AddSeconds(unixtime);
        astanaTime = DateTime.SpecifyKind(astanaTime, DateTimeKind.Local);
        var dateString = astanaTime.ToString("o");
        return dateString;
    }

    public static string UnixTimeToLocalString(int unixtime, string language)
    {
        var datetime = UnixTimeToDateTime(unixtime);
        switch (language)
        {
            case "kz":
            case "tote":
            case "latyn":
            {
                var month = datetime.ToString("HH:mm, dd MMMM yyyy", new CultureInfo("kk-KZ"));
                switch (language)
                {
                    case "tote":
                    {
                        return Cyrl2ToteHelper.Cyrl2Tote(month);
                    }
                    case "latyn":
                    {
                        return Cyrl2LatynHelper.Cyrl2Latyn(month);
                    }
                    default:
                    {
                        return month;
                    }
                }
            }
            case "ru":
            {
                return datetime.ToString("HH:mm, dd MMMM yyyy", new CultureInfo("ru-RU"));
            }
            case "zh-cn":
            {
                return datetime.ToString("HH:mm, dd MMMM yyyy", new CultureInfo("zh-CN"));
            }
            case "tr":
            {
                return datetime.ToString("HH:mm, dd MMMM yyyy", new CultureInfo("tr-TR"));
            }
            default:
            {
                return datetime.ToString("HH:mm, dd MMMM yyyy", new CultureInfo("en-US"));
            }
        }
    }
    
    public static string UnixTimeToString(int unixTime)
    {
        return UnixTimeToString(UnixTimeToDateTime(unixTime));
    }

    public static string UnixTimeToString(DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy HH:mm");
    }
}