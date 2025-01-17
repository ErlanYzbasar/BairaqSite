using System.Net.Mail;
using System.Text.RegularExpressions;

namespace COMMON;

public static class RegexHelper
{
    #region Елхат анықтау   +IsEmail(string email)

    static public bool IsEmail(string email)
    {
        try
        {
            var m = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    #endregion

    #region Латын әріпін анықтау +IsLatinString(string str)

    public static bool IsLatinString(string str)
    {
        return Regex.IsMatch(str, @"^[a-zA-Z0-9-]+$");
    }

    #endregion

    #region Жалғаным екенін анықтау + IsUrl(string str)

    public static bool IsUrl(string addressString)
    {
        Uri result = null;
        return Uri.TryCreate(addressString, UriKind.RelativeOrAbsolute, out result);
    }

    #endregion

    #region Кілт тізбегін анықтау +IsLocalString(string str)

    public static bool IsLocalString(string str)
    {
        if (str.Length < 4) return false;
        if (!str.Substring(0, 3).Equals("ls_")) return false;
        return Regex.IsMatch(str, @"^[a-zA-Z0-9_]+$");
    }

    #endregion

    #region Телефон нөмірін анықтау   +IsPhoneNumber(string phoneNumber, out string phoneNumber)

    static public bool IsPhoneNumber(string phone, out string phoneNumber)
    {
        phoneNumber = string.Empty;
        if (phone.StartsWith("1") && phone.Length == 11)
        {
            phoneNumber = "+86" + phone;
            return true;
        }

        if (phone.StartsWith("7") && phone.Length == 10)
        {
            phoneNumber = "+7" + phone;
            return true;
        }

        if (phone.StartsWith("77") && phone.Length == 11)
        {
            phoneNumber = "+" + phone;
            return true;
        }

        if (phone.StartsWith("87") && phone.Length == 11)
        {
            phoneNumber = "+7" + phone.Substring(1);
            return true;
        }

        if ((phone.StartsWith("+861") && phone.Length == 14) || (phone.StartsWith("+77") && phone.Length == 12))
        {
            phoneNumber = phone;
            return true;
        }

        return false;
    }

    #endregion

    #region Түс мәнінің дұрыстығын анықтау   +IsHexColorString(string str)

    static public bool IsHexColorString(string str)
    {
        return Regex.IsMatch(str, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
    }

    #endregion
}