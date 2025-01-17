using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace COMMON;

public class Cyrl2ToteHelper
{
    enum Sound
    {
        Vowel, //Дауысты дыбыс
        Consonant, //Дауыссыз дыбыс
        Unknown //Белгісіз
    }

    private readonly static string[] CyrlChars =
    {
        "А", "Ә", "Ə", "Б", "В", "Г", "Ғ", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Қ", "Л", "М", "Н", "Ң", "О", "Ө",
        "Ɵ", "П", "Р", "С", "Т", "У", "Ұ", "Ү", "Ф", "Х", "Һ", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "І", "Ь", "Э", "Ю", "Я",
        "-"
    };

    private static readonly Dictionary<string, string> DialectWordsDic = new()
    {
        { "قر", "ق ر" }, { "جحر", "ج ح ر" }, { "جشس", "ج ش س" }, { "شۇار", "ش ۇ ا ر" }, { "باق", "ب ا ق" },
        { "ءباسپاسوز", "باسپا ءسوز" }, { "قىتاي", "جۇڭگو" }
    };

    #region Кирилшені төте жазуға сәйкестендіру

    public static string Cyrl2Tote(string cyrlText)
    {
        cyrlText = CopycatCyrlToOriginalCyrl(cyrlText);
        cyrlText += ".";
        cyrlText = WebUtility.HtmlDecode(cyrlText); //Сайт сәйкестендіруге қолданбаса алып тастауға болады
        var chars = cyrlText.ToCharArray().Select(x => x.ToString()).ToArray();
        var length = chars.Length;
        var toteStrs = new string[length];
        var prevSound = Sound.Unknown;
        var cyrlWord = string.Empty;
        for (var i = 0; i < length; i++)
        {
            if (!CyrlChars.Contains(chars[i].ToUpper()))
            {
                if (!string.IsNullOrEmpty(cyrlWord))
                {
                    var wordLength = cyrlWord.Length;
                    var toteChars = new string[wordLength];
                    var j = i - wordLength;
                    var tIndex = 0;
                    for (; j < i; j++, tIndex++)
                    {
                        if (j + 1 < length)
                        {
                            var key = string.Concat(chars[j], chars[j + 1]);
                            switch (key.ToLower())
                            {
                                case "ия":
                                {
                                    toteChars[tIndex] = "يا";
                                    j += 1;
                                    continue;
                                }
                                case "йя":
                                {
                                    toteChars[tIndex] = "ييا";
                                    j += 1;
                                    continue;
                                }
                                case "ию":
                                {
                                    toteChars[tIndex] = "يۋ";
                                    j += 1;
                                    continue;
                                }
                                case "йю":
                                {
                                    toteChars[tIndex] = "يۋ";
                                    j += 1;
                                    continue;
                                }
                                case "сц":
                                {
                                    toteChars[tIndex] = "س";
                                    j += 1;
                                    continue;
                                }
                                case "тч":
                                {
                                    toteChars[tIndex] = "چ";
                                    j += 1;
                                    continue;
                                }
                                case "ий":
                                {
                                    toteChars[tIndex] = "ي";
                                    j += 1;
                                    continue;
                                }
                                case "ХХ":
                                {
                                    toteChars[tIndex] = "ХХ";
                                    j += 1;
                                    continue;
                                }
                            }
                        }

                        switch (chars[j].ToLower())
                        {
                            case "я":
                            {
                                toteChars[tIndex] = prevSound == Sound.Consonant ? "ءا" : "يا";
                            }
                                break;
                            case "ю":
                            {
                                toteChars[tIndex] = prevSound == Sound.Consonant ? "ءۇ" : "يۋ";
                            }
                                break;
                            case "щ":
                            {
                                toteChars[tIndex] = "شش";
                            }
                                break;
                            case "э":
                            {
                                toteChars[tIndex] = "ە";
                            }
                                break;
                            case "а":
                            {
                                toteChars[tIndex] = "ا";
                            }
                                break;
                            case "б":
                            {
                                toteChars[tIndex] = "ب";
                            }
                                break;
                            case "ц":
                            {
                                toteChars[tIndex] = "س";
                            }
                                break;
                            case "д":
                            {
                                toteChars[tIndex] = "د";
                            }
                                break;
                            case "е":
                            {
                                toteChars[tIndex] = "ە";
                            }
                                break;
                            case "ф":
                            {
                                toteChars[tIndex] = "ف";
                            }
                                break;
                            case "г":
                            {
                                toteChars[tIndex] = "گ";
                            }
                                break;
                            case "х":
                            {
                                toteChars[tIndex] = "ح";
                            }
                                break;
                            case "Һ":
                            case "һ":
                            {
                                toteChars[tIndex] = "ھ";
                            }
                                break;
                            case "І":
                            case "і":
                            {
                                toteChars[tIndex] = "ءى";
                            }
                                break;
                            case "и":
                            case "й":
                            {
                                toteChars[tIndex] = "ي";
                            }
                                break;
                            case "к":
                            {
                                toteChars[tIndex] = "ك";
                            }
                                break;
                            case "л":
                            {
                                toteChars[tIndex] = "ل";
                            }
                                break;
                            case "м":
                            {
                                toteChars[tIndex] = "م";
                            }
                                break;
                            case "н":
                            {
                                toteChars[tIndex] = "ن";
                            }
                                break;
                            case "о":
                            {
                                toteChars[tIndex] = "و";
                            }
                                break;
                            case "п":
                            {
                                toteChars[tIndex] = "پ";
                            }
                                break;
                            case "қ":
                            {
                                toteChars[tIndex] = "ق";
                            }
                                break;
                            case "р":
                            {
                                toteChars[tIndex] = "ر";
                            }
                                break;
                            case "с":
                            {
                                toteChars[tIndex] = "س";
                            }
                                break;
                            case "т":
                            {
                                toteChars[tIndex] = "ت";
                            }
                                break;
                            case "ұ":
                            {
                                toteChars[tIndex] = "ۇ";
                            }
                                break;
                            case "в":
                            {
                                toteChars[tIndex] = "ۆ";
                            }
                                break;
                            case "у":
                            {
                                toteChars[tIndex] = "ۋ";
                            }
                                break;
                            case "ы":
                            {
                                toteChars[tIndex] = "ى";
                            }
                                break;
                            case "з":
                            {
                                toteChars[tIndex] = "ز";
                            }
                                break;
                            case "ә":
                            {
                                toteChars[tIndex] = "ءا";
                            }
                                break;
                            case "ё":
                            case "ө":
                            {
                                toteChars[tIndex] = "ءو";
                            }
                                break;
                            case "ү":
                            {
                                toteChars[tIndex] = "ءۇ";
                            }
                                break;
                            case "ч":
                            {
                                toteChars[tIndex] = "چ";
                            }
                                break;
                            case "ғ":
                            {
                                toteChars[tIndex] = "ع";
                            }
                                break;
                            case "ш":
                            {
                                toteChars[tIndex] = "ش";
                            }
                                break;
                            case "ж":
                            {
                                toteChars[tIndex] = "ج";
                            }
                                break;
                            case "ң":
                            {
                                toteChars[tIndex] = "ڭ";
                            }
                                break;
                            case "ь":
                            {
                                toteChars[tIndex] = "";
                            }
                                break;
                            case "Ь":
                            {
                                toteChars[tIndex] = "";
                            }
                                break;
                            case "ъ":
                            {
                                toteChars[tIndex] = "";
                            }
                                break;
                            case "Ъ":
                            {
                                toteChars[tIndex] = "";
                            }
                                break;
                            case "¬":
                            {
                                toteChars[tIndex] = "";
                            }
                                break;
                            default:
                            {
                                toteChars[tIndex] = chars[j] != "" ? chars[j] : "";
                            }
                                break;
                        }
                    }

                    var toteWord = string.Concat(toteChars);
                    if (toteWord.Contains("ء"))
                    {
                        toteWord = toteWord.Replace("ء", "");
                        if (!(toteWord.Contains("ك") || toteWord.Contains("گ") || toteWord.Contains("ە")))
                        {
                            toteWord = "ء" + toteWord;
                        }
                    }

                    toteWord = ReplaceDialectWords(toteWord);
                    toteStrs[i - wordLength] = toteWord;
                    cyrlWord = string.Empty;
                }

                switch (chars[i])
                {
                    case ",":
                    {
                        toteStrs[i] = "،";
                    }
                        break;
                    case "?":
                    {
                        toteStrs[i] = "؟";
                    }
                        break;
                    case ";":
                    {
                        toteStrs[i] = "؛";
                    }
                        break;
                    default:
                    {
                        toteStrs[i] = chars[i];
                    }
                        break;
                }

                prevSound = Sound.Unknown;
                continue;
            }

            cyrlWord += chars[i];
            prevSound = Sound.Unknown;
        }

        toteStrs[length - 1] = "";
        return string.Concat(toteStrs);
    }

    #endregion
    
    
    public static string Tote2Cyrl(string toteText)
    {
        var toteToCyrlMap = new Dictionary<string, string>
        {
            { "يا", "я" }, { "يۋ", "ю" }, { "ش", "ш" }, { "چ", "ч" },
            { "ءا", "ә" }, { "ا", "а" }, { "ب", "б" }, { "د", "д" },
            { "ە", "е" }, { "ف", "ф" }, { "گ", "г" }, { "ح", "х" },
            { "ھ", "һ" }, { "ءى", "і" }, { "ى", "ы" }, { "ي", "и" },
            { "ك", "к" }, { "ل", "л" }, { "م", "м" }, { "ن", "н" },
            { "ڭ", "ң" }, { "و", "о" }, { "پ", "п" }, { "ق", "қ" },
            { "ر", "р" }, { "س", "с" }, { "ت", "т" }, { "ۇ", "ұ" },
            { "ۋ", "у" }, { "ۆ", "в" }, { "ز", "з" }, { "ءو", "ө" },
            { "ءۇ", "ү" }, { "ج", "ж" }, { "ع", "ғ" }, { "", "" },
            { "،", "," }, { "؟", "?" }, { "؛", ";" }, { "-", "-" }
        };

        var multiCharSequences = new[] { "يا", "يۋ", "ءا", "ءو", "ءۇ", "ڭ", "ح", "ش", "چ" };

        var result = new StringBuilder();
        for (int i = 0; i < toteText.Length; i++)
        {
            string currentChar = toteText[i].ToString();

            string match = null;
            foreach (var seq in multiCharSequences)
            {
                if (i + seq.Length <= toteText.Length && toteText.Substring(i, seq.Length) == seq)
                {
                    match = seq;
                    break;
                }
            }

            if (match != null)
            {
                result.Append(toteToCyrlMap[match]);
                i += match.Length - 1; 
            }
            else
            {
                result.Append(toteToCyrlMap.ContainsKey(currentChar) ? toteToCyrlMap[currentChar] : currentChar);
            }
        }

        return result.ToString();
    }


    #region Жат кирлл әріптерін төл кирлларыпыне айналдыру +CopycatCyrlToOriginalCyrl(string cyrlText)

    private static string CopycatCyrlToOriginalCyrl(string cyrlText)
    {
        return new StringBuilder(cyrlText)
            .Replace("Ə", "Ә")
            .Replace("ə", "ә")
            .Replace("Ɵ", "Ө")
            .Replace("ɵ", "ө").ToString();
    }

    #endregion

    #region Диалект сөздерді аустыру +ReplaceDialectWords(string word)

    private static string ReplaceDialectWords(string word)
    {
        if (DialectWordsDic.ContainsKey(word)) return DialectWordsDic[word];
        word = Regex.Replace(word, @"\w(ۇلى)\s|\w(ۇلى$)",
            m => string.Format("{0}", m.Groups[0].Value.Replace("ۇلى", " ۇلى")), RegexOptions.RightToLeft);
        word = Regex.Replace(word, @"\w(ۇلىنىڭ)\s|\w(ۇلىنىڭ$)",
            m => string.Format("{0}", m.Groups[0].Value.Replace("ۇلىنىڭ", " ۇلىنىڭ")), RegexOptions.RightToLeft);
        word = Regex.Replace(word, @"\w(قىزى)\s|\w(قىزى$)",
            m => string.Format("{0}", m.Groups[0].Value.Replace("قىزى", " قىزى")), RegexOptions.RightToLeft);
        word = Regex.Replace(word, @"\w(قىزىنىڭ)\s|\w(قىزىنىڭ$)",
            m => string.Format("{0}", m.Groups[0].Value.Replace("قىزىنىڭ", " قىزىنىڭ")), RegexOptions.RightToLeft);
        word = Regex.Replace(word, @"\w(ەۆ)\s|\w(ەۆ)",
            m => string.Format("{0}", m.Groups[0].Value.Replace("ەۆ", "يەۆ")), RegexOptions.RightToLeft);
        return word;
    }

    #endregion
}