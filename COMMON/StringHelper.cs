using System.Text;
using System.Text.RegularExpressions;

namespace COMMON;

public static class StringHelper
{
    #region Kaz2Lat for URL functions

    //Kyrylshe maqala taqiribina negizdelip latinsha URL zhasaw
    public static string Kaz2LatForUrl(string cyrlText)
    {
        var tmp = cyrlText.Trim().ToLower().ToCharArray();
        var sb = new StringBuilder();

        for (var i = 0; i < tmp.Length; i++)
        {
            switch (tmp[i])
            {
                case 'ю':
                    sb.Append("iu");
                    break;
                case 'я':
                    sb.Append("ia");
                    break;
                case 'ё':
                    sb.Append("io");
                    break;
                case 'э':
                    sb.Append('e');
                    break;
                case 'ц':
                case 'с':
                    sb.Append('s');
                    break;
                case 'м':
                    sb.Append('m');
                    break;
                case 'й':
                case 'і':
                case 'и':
                    sb.Append('i');
                    break;
                case 'т':
                    sb.Append('t');
                    break;
                case 'б':
                    sb.Append('b');
                    break;
                case 'ф':
                    sb.Append('f');
                    break;
                case 'ы':
                    sb.Append('y');
                    break;
                case 'в':
                    sb.Append('v');
                    break;
                case 'а':
                case 'ә':
                    sb.Append('a');
                    break;
                case 'п':
                    sb.Append('p');
                    break;
                case 'р':
                    sb.Append('r');
                    break;
                case 'о':
                case 'ө':
                    sb.Append('o');
                    break;
                case 'л':
                    sb.Append('l');
                    break;
                case 'д':
                    sb.Append('d');
                    break;
                case 'ж':
                    sb.Append('j');
                    break;
                case 'ү':
                case 'ұ':
                case 'у':
                    sb.Append('u');
                    break;
                case 'к':
                    sb.Append('k');
                    break;
                case 'е':
                    sb.Append('e');
                    break;
                case 'н':
                    sb.Append('n');
                    break;
                case 'г':
                    sb.Append('g');
                    break;
                case 'ш':
                case 'щ':
                    sb.Append("sh");
                    break;
                case 'з':
                    sb.Append('z');
                    break;
                case 'х':
                case 'һ':
                    sb.Append('h');
                    break;
                case 'ң':
                    sb.Append('n');
                    break;
                case 'ғ':
                    sb.Append('g');
                    break;
                case 'қ':
                    sb.Append('q');
                    break;
                case 'ч':
                    sb.Append("ch");
                    break;
                case ' ':
                case '-':
                    sb.Append('-');
                    break;
                default:
                    if ((tmp[i] > 96) && (tmp[i] < 123))
                        sb.Append(tmp[i]);
                    else if ((tmp[i] > 47) && (tmp[i] < 58))
                        sb.Append(tmp[i]);
                    else
                        sb.Append("");
                    break;
            }
        }

        return Regex.Replace(sb.ToString(), @"\-+", "-");
    }

    #endregion

    public static string SymbolReplace(string str)
    {
        return string.IsNullOrEmpty(str)
            ? str
            : str.Replace("<<", "«").Replace("<", "«").Replace(">>", "»").Replace(">", "»");
    }

    #region Get Sub Text +GetSubText(string text, int length)

    public static string GetSubText(string text, int length)
    {
        if (text.Length <= length) return text;
        text = text[..(length - 3)];
        var lastWhitespaceIndex = text.LastIndexOf(" ", StringComparison.Ordinal);
        if (lastWhitespaceIndex > 0)
        {
            text = text[..lastWhitespaceIndex];
        }

        string[] symbols = { ",", "?", "!", ":", ".", " ", "\"", "%", "'" };
        if (symbols.Any(x => x.Equals(text[^1])))
        {
            text = text[..(text.Length - 2)];
        }

        return text+"...";
    }

    #endregion

   public static string GetRealLatynUrl(string input)
    {
        string[] parts = input.Split('-');
        if (parts.Length > 1 && int.TryParse(parts[0], out _))
        {
            return string.Join("-", parts, 1, parts.Length - 1);
        }
        return input;
    }
}